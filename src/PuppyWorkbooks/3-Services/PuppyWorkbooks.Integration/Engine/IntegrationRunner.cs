using System.Collections;
using System.Globalization;
using System.Text.Json;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class IntegrationRunner(IntegrationRunnerOptions? options = null)
{
    private readonly WorkbookInterpreter _interpreter = new();
    private readonly IntegrationRunnerOptions _options = options ?? new();

    public async Task<IntegrationResult> RunAsync(IntegrationDefinition definition, CancellationToken cancellationToken = default)
    {
        var input = definition.Steps.OfType<InputStep>().FirstOrDefault();
        if (input is null) throw new InvalidOperationException("An integration must contain an IOInput step.");
        await using var inputProvider = CreateInput(input);
        var outputs = new List<IOutputProvider>();
        try
        {
            foreach (var output in definition.Steps.OfType<OutputStep>())
            {
                outputs.Add(CreateOutput(output));
            }
            var reduceSteps = GetReduceSteps(definition.Steps).ToList();
            var reduceStates = reduceSteps.ToDictionary(step => step, step => ValueBinder.ParseInitialState(step.InitialStateJson));
            var outputSteps = definition.Steps.OfType<OutputStep>().ToList();
            var deferOutput = reduceSteps.Count > 0;
            long read = 0, written = 0, excluded = 0;
            IntegrationRecord? final = null;
            var isDebug = _options.Debug;
            var debugRows = isDebug ? new List<IntegrationDebugRow>() : null;

            await foreach (var sourceRecord in inputProvider.ReadAsync(cancellationToken))
            {
                read++;
                var record = sourceRecord;
                var rowDebug = IntegrationDebugger.InitializeDebugRow(isDebug, sourceRecord, debugRows);

                foreach (var step in definition.Steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    switch (step)
                    {
                        case InputStep inputStep:
                            IntegrationDebugger.DebugInputStep(rowDebug, inputStep, record);
                            break;

                        case MapStep map:
                            var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, cancellationToken);
                            record = mapRecord;
                            IntegrationDebugger.DebugWorksheetStep(mapDebug, rowDebug);
                            break;

                        case FilterStep filter:
                            var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, cancellationToken);
                            IntegrationDebugger.DebugWorksheetStep(filterDebug, rowDebug);
                            if (!keepFilter)
                            {
                                excluded++;
                                goto NextRecord;
                            }
                            break;

                        case ReduceStep reduce:
                            var (reduceRecord, nextState, reduceDebug) = await ExecuteReduceStep(reduce, record, reduceStates[reduce], isDebug, cancellationToken);
                            reduceStates[reduce] = nextState;
                            record = reduceRecord;
                            IntegrationDebugger.DebugWorksheetStep(reduceDebug, rowDebug);
                            break;

                        case SwitchStep @switch:
                            var (switchRecord, switchExcluded, switchDebug) = await ExecuteSwitchStep(@switch, record, reduceStates, isDebug, cancellationToken);
                            record = switchRecord;
                            IntegrationDebugger.DebugWorksheetStep(switchDebug, rowDebug);
                            if (switchExcluded)
                            {
                                excluded++;
                                goto NextRecord;
                            }
                            break;

                        case OutputStep output when !deferOutput:
                            IntegrationDebugger.DebugOutputStep(rowDebug, output, record);
                            var status = await outputs[outputSteps.IndexOf(output)].WriteAsync(record, cancellationToken);
                            record[output.Id + ".Status"] = status.Succeeded;
                            record[output.Id + ".StatusMessage"] = status.Message;
                            record[output.Id + ".AffectedRows"] = status.AffectedRows;
                            written += status.AffectedRows;
                            break;

                        case OutputStep output when deferOutput:
                            IntegrationDebugger.DebugOutputStep(rowDebug, output, record);
                            break;
                    }
                }
                final = record;
                NextRecord:;
            }

            if (deferOutput && final is not null)
            {
                for (var outputIndex = 0; outputIndex < outputSteps.Count; outputIndex++)
                {
                    var output = outputSteps[outputIndex];
                    var status = await outputs[outputIndex].WriteAsync(final, cancellationToken);
                    final[output.Id + ".Status"] = status.Succeeded;
                    final[output.Id + ".StatusMessage"] = status.Message;
                    final[output.Id + ".AffectedRows"] = status.AffectedRows;
                    written += status.AffectedRows;
                }
            }

            return new IntegrationResult(read, written, excluded, final)
            {
                DebugData = debugRows
            };
        }
        finally
        {
            foreach (var output in outputs) await output.DisposeAsync();
        }
    }

    private IInputProvider CreateInput(InputStep step)
    {
        if (ShouldUseMock(step))
        {
            return CreateMockInput(step);
        }

        return step.Kind switch
        {
            InputKind.CSVReader when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvInputProvider(step.FilePath),
            InputKind.SqlReader when _options.ConnectionFactory is not null => new SqlInputProvider(_options.ConnectionFactory(step.ConnectionString), step.Query),
            _ => throw new InvalidOperationException("SQL input requires ConnectionFactory; unsupported or missing input configuration.")
        };
    }

    private bool ShouldUseMock(IntegrationStep step)
    {
        if (string.IsNullOrWhiteSpace(_options.UseMockDataForSteps))
            return false;

        var setting = _options.UseMockDataForSteps.Trim();
        if (string.Equals(setting, "ALL", StringComparison.OrdinalIgnoreCase))
            return true;

        var stepIds = setting.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return stepIds.Any(id => string.Equals(id, step.Id, StringComparison.OrdinalIgnoreCase));
    }

    private static IInputProvider CreateMockInput(InputStep step)
    {
        if (!string.IsNullOrWhiteSpace(step.MockCsvFilePath))
        {
            return new CsvInputProvider(step.MockCsvFilePath);
        }

        var inlineCsv = !string.IsNullOrWhiteSpace(step.MockCsv) ? step.MockCsv : step.MockData;
        if (!string.IsNullOrWhiteSpace(inlineCsv))
        {
            return CsvInputProvider.FromText(inlineCsv.Trim());
        }

        throw new InvalidOperationException($"Mock data was requested for input step '{step.Id}', but no mock CSV data or file path was defined in the step.");
    }

    private IOutputProvider CreateOutput(OutputStep step)
    {
        if (ShouldUseMock(step))
        {
            return new MockOutputProvider();
        }

        return step.Kind switch
        {
            OutputKind.CSVWriter when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvOutputProvider(step.FilePath),
            OutputKind.SqlWriter when _options.ConnectionFactory is not null => new SqlOutputProvider(_options.ConnectionFactory(step.ConnectionString), step.TableName, step.Query),
            _ => throw new InvalidOperationException("SQL output requires ConnectionFactory; unsupported or missing output configuration.")
        };
    }

    private async Task<(IntegrationRecord Record, IntegrationStepDebug? Debug)> ExecuteMapStep(
        MapStep step,
        IntegrationRecord record,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var stepDebug = isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Map",
                Cells = values
            }
            : null;
        return (new IntegrationRecord(values), stepDebug);
    }

    private async Task<(bool ShouldKeepRecord, IntegrationStepDebug? Debug)> ExecuteFilterStep(
        FilterStep step,
        IntegrationRecord record,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var stepDebug = IntegrationDebugger.InitializeFilterDebug(step, isDebug, values);
        var last = values.Values.LastOrDefault();
        var keep = ValueBinder.ToBoolean(last);
        var shouldKeep = step.KeepWhenTrue ? keep : !keep;
        return (shouldKeep, stepDebug);
    }

    private async Task<(IntegrationRecord Record, object? State, IntegrationStepDebug? Debug)> ExecuteReduceStep(
        ReduceStep step,
        IntegrationRecord record,
        object? state,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["State"] = state });
        var stepDebug = IntegrationDebugger.InitializeReduceDebugStep(step, isDebug, values);
        var next = values.TryGetValue(step.OutputField, out var fieldVal)
            ? fieldVal
            : values.Values.LastOrDefault();
        return (ValueBinder.CreateStateRecord(step.OutputField, next), next, stepDebug);
    }

    private async Task<(IntegrationRecord Record, bool Excluded, IntegrationStepDebug? Debug)> ExecuteSwitchStep(
        SwitchStep step,
        IntegrationRecord record,
        Dictionary<ReduceStep, object?> reduceStates,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var switchDebug = IntegrationDebugger.InitializeSwitchStepDebug(step, isDebug, values);

        foreach (var branch in step.Branches)
        {
            if (string.IsNullOrWhiteSpace(branch.WorkCell))
                throw new InvalidOperationException($"Switch '{step.Id}' has a branch without a WorkCell attribute.");
            if (!values.TryGetValue(branch.WorkCell, out var value))
                throw new InvalidOperationException($"Switch '{step.Id}' does not contain a worksheet cell named '{branch.WorkCell}'.");

            var conditionMet = ValueBinder.ToBoolean(value);
            var branchDebug = IntegrationDebugger.IntegrationBranchDebug(isDebug, branch, conditionMet, switchDebug);

            if (!conditionMet) continue;

            foreach (var branchStep in branch.Steps)
            {
                token.ThrowIfCancellationRequested();
                switch (branchStep)
                {
                    case MapStep map:
                        var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, token);
                        record = mapRecord;
                        branchDebug?.AddStep(mapDebug);
                        break;

                    case FilterStep filter:
                        var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, token);
                        branchDebug?.AddStep(filterDebug);
                        if (!keepFilter) return (record, true, switchDebug);
                        break;

                    case ReduceStep reduce:
                        var (reduceRecord, newState, reduceDebug) = await ExecuteReduceStep(reduce, record, reduceStates[reduce], isDebug, token);
                        reduceStates[reduce] = newState;
                        record = reduceRecord;
                        branchDebug?.AddStep(reduceDebug);
                        break;

                    case SwitchStep nested:
                        var (nestedRecord, nestedExcluded, nestedDebug) = await ExecuteSwitchStep(nested, record, reduceStates, isDebug, token);
                        record = nestedRecord;
                        branchDebug?.AddStep(nestedDebug);
                        if (nestedExcluded) return (record, true, switchDebug);
                        break;

                    case InputStep or OutputStep:
                        throw new InvalidOperationException("Input and output steps are not valid inside a switch branch.");
                }
            }
        }
        return (record, false, switchDebug);
    }

    private static IEnumerable<ReduceStep> GetReduceSteps(IEnumerable<IntegrationStep> steps)
    {
        foreach (var step in steps)
        {
            if (step is ReduceStep reduce) yield return reduce;
            if (step is SwitchStep @switch)
                foreach (var branch in @switch.Branches)
                    foreach (var nested in GetReduceSteps(branch.Steps)) yield return nested;
        }
    }

    private async Task<Dictionary<string, object?>> EvaluateCells(WorkSheet? worksheet,
        IntegrationRecord record, CancellationToken token,
        IReadOnlyDictionary<string, object?>? additionalBindings = null)
    {
        if (worksheet is null) throw new InvalidOperationException("A worksheet is required for this step.");
        var outputCells = worksheet.Cells
            .Where(c => !string.IsNullOrWhiteSpace(c.Formula))
            .ToList();
        var copy = new WorkSheet { Name = worksheet.Name, Cells =
            [.. worksheet.Cells.Select(c => new WorkCell(c.Id, c.Name, c.Formula, c.Comments))],
            Variables = new Dictionary<string, string>(worksheet.Variables, StringComparer.OrdinalIgnoreCase)
        };
        ValueBinder.BindRecord(copy, record);
        if (additionalBindings is not null)
            foreach (var binding in additionalBindings)
                ValueBinder.BindValue(copy, binding.Key, binding.Value);
        var values = await _interpreter.EvaluateCellsAsync(copy, token);
        var ordered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in outputCells)
        {
            if (values.TryGetValue(cell.Name, out var cellVal))
            {
                ordered[cell.Name] = cellVal;
            }
        }
        return ordered;
    }
}
