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
            foreach (var output in definition.Steps.OfType<OutputStep>()) outputs.Add(CreateOutput(output));
            var reduceSteps = GetReduceSteps(definition.Steps).ToList();
            var reduceStates = reduceSteps.ToDictionary(step => step, step => ParseInitialState(step.InitialStateJson));
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
                IntegrationDebugRow? rowDebug = null;
                if (isDebug)
                {
                    rowDebug = new IntegrationDebugRow
                    {
                        InputRow = new Dictionary<string, object?>(sourceRecord.Values, StringComparer.OrdinalIgnoreCase)
                    };
                    debugRows!.Add(rowDebug);
                }

                foreach (var step in definition.Steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    switch (step)
                    {
                        case InputStep inputStep:
                            if (rowDebug is not null)
                            {
                                rowDebug.Steps.Add(new IntegrationStepDebug
                                {
                                    Id = inputStep.Id,
                                    StepType = "IOInput",
                                    Cells = new Dictionary<string, object?>(sourceRecord.Values, StringComparer.OrdinalIgnoreCase)
                                });
                            }
                            break;

                        case MapStep map:
                            var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, cancellationToken);
                            record = mapRecord;
                            if (mapDebug is not null) rowDebug?.Steps.Add(mapDebug);
                            break;

                        case FilterStep filter:
                            var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, cancellationToken);
                            if (filterDebug is not null) rowDebug?.Steps.Add(filterDebug);
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
                            if (reduceDebug is not null) rowDebug?.Steps.Add(reduceDebug);
                            break;

                        case SwitchStep @switch:
                            var (switchRecord, switchExcluded, switchDebug) = await ExecuteSwitchStep(@switch, record, reduceStates, isDebug, cancellationToken);
                            record = switchRecord;
                            if (switchDebug is not null) rowDebug?.Steps.Add(switchDebug);
                            if (switchExcluded)
                            {
                                excluded++;
                                goto NextRecord;
                            }
                            break;

                        case OutputStep output when !deferOutput:
                            if (rowDebug is not null)
                            {
                                rowDebug.Steps.Add(new IntegrationStepDebug
                                {
                                    Id = output.Id,
                                    StepType = "IOOutput",
                                    Cells = new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase)
                                });
                            }
                            var status = await outputs[outputSteps.IndexOf(output)].WriteAsync(record, cancellationToken);
                            record[output.Id + ".Status"] = status.Succeeded;
                            record[output.Id + ".StatusMessage"] = status.Message;
                            record[output.Id + ".AffectedRows"] = status.AffectedRows;
                            written += status.AffectedRows;
                            break;

                        case OutputStep output when deferOutput:
                            if (rowDebug is not null)
                            {
                                rowDebug.Steps.Add(new IntegrationStepDebug
                                {
                                    Id = output.Id,
                                    StepType = "IOOutput",
                                    Cells = new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase)
                                });
                            }
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

    private IInputProvider CreateInput(InputStep step) => step.Kind switch
    {
        InputKind.CSVReader when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvInputProvider(step.FilePath),
        InputKind.SqlReader when _options.ConnectionFactory is not null => new SqlInputProvider(_options.ConnectionFactory(step.ConnectionString), step.Query),
        _ => throw new InvalidOperationException("SQL input requires ConnectionFactory; unsupported or missing input configuration.")
    };

    private IOutputProvider CreateOutput(OutputStep step) => step.Kind switch
    {
        OutputKind.CSVWriter when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvOutputProvider(step.FilePath),
        OutputKind.SqlWriter when _options.ConnectionFactory is not null => new SqlOutputProvider(_options.ConnectionFactory(step.ConnectionString), step.TableName, step.Query),
        _ => throw new InvalidOperationException("SQL output requires ConnectionFactory; unsupported or missing output configuration.")
    };

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

    private async Task<(bool ShouldKeep, IntegrationStepDebug? Debug)> ExecuteFilterStep(
        FilterStep step,
        IntegrationRecord record,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var stepDebug = isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Filter",
                Cells = values
            }
            : null;
        var last = values.Values.LastOrDefault();
        var keep = ToBoolean(last);
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
        var stepDebug = isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Reduce",
                Cells = values
            }
            : null;
        var next = values.TryGetValue(step.OutputField, out var fieldVal)
            ? fieldVal
            : values.Values.LastOrDefault();
        return (CreateStateRecord(step.OutputField, next), next, stepDebug);
    }

    private async Task<(IntegrationRecord Record, bool Excluded, IntegrationStepDebug? Debug)> ExecuteSwitchStep(
        SwitchStep step,
        IntegrationRecord record,
        Dictionary<ReduceStep, object?> reduceStates,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var switchDebug = isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Switch",
                Cells = values,
                Branches = []
            }
            : null;

        foreach (var branch in step.Branches)
        {
            if (string.IsNullOrWhiteSpace(branch.WorkCell))
                throw new InvalidOperationException($"Switch '{step.Id}' has a branch without a WorkCell attribute.");
            if (!values.TryGetValue(branch.WorkCell, out var value))
                throw new InvalidOperationException($"Switch '{step.Id}' does not contain a worksheet cell named '{branch.WorkCell}'.");

            var conditionMet = ToBoolean(value);
            var branchDebug = isDebug
                ? new IntegrationBranchDebug
                {
                    WorkCell = branch.WorkCell,
                    Executed = conditionMet,
                    Steps = []
                }
                : null;

            if (branchDebug is not null)
            {
                switchDebug!.Branches!.Add(branchDebug);
            }

            if (!conditionMet) continue;

            foreach (var branchStep in branch.Steps)
            {
                token.ThrowIfCancellationRequested();
                switch (branchStep)
                {
                    case MapStep map:
                        var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, token);
                        record = mapRecord;
                        if (mapDebug is not null) branchDebug?.Steps.Add(mapDebug);
                        break;

                    case FilterStep filter:
                        var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, token);
                        if (filterDebug is not null) branchDebug?.Steps.Add(filterDebug);
                        if (!keepFilter) return (record, true, switchDebug);
                        break;

                    case ReduceStep reduce:
                        var (reduceRecord, newState, reduceDebug) = await ExecuteReduceStep(reduce, record, reduceStates[reduce], isDebug, token);
                        reduceStates[reduce] = newState;
                        record = reduceRecord;
                        if (reduceDebug is not null) branchDebug?.Steps.Add(reduceDebug);
                        break;

                    case SwitchStep nested:
                        var (nestedRecord, nestedExcluded, nestedDebug) = await ExecuteSwitchStep(nested, record, reduceStates, isDebug, token);
                        record = nestedRecord;
                        if (nestedDebug is not null) branchDebug?.Steps.Add(nestedDebug);
                        if (nestedExcluded) return (record, true, switchDebug);
                        break;

                    case InputStep or OutputStep:
                        throw new InvalidOperationException("Input and output steps are not valid inside a switch branch.");
                }
            }
        }
        return (record, false, switchDebug);
    }

    private static bool ToBoolean(object? value) => value is bool b
        ? b
        : bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;

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

    private static IntegrationRecord CreateStateRecord(string outputField, object? state)
    {
        if (state is IDictionary dictionary)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry item in dictionary)
                values[Convert.ToString(item.Key, CultureInfo.InvariantCulture)!] = item.Value;
            return new IntegrationRecord(values);
        }

        if (state is JsonElement { ValueKind: JsonValueKind.Object } jsonObject)
        {
            var values = jsonObject.Deserialize<Dictionary<string, object?>>() ?? [];
            return new IntegrationRecord(values);
        }

        return new IntegrationRecord(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [outputField] = state
        });
    }

    private static object? ParseInitialState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private async Task<object?> Evaluate(WorkSheet? worksheet, IntegrationRecord record, CancellationToken token,
        IReadOnlyDictionary<string, object?>? additionalBindings = null)
    {
        if (worksheet is null) throw new InvalidOperationException("A worksheet is required for this step.");
        var copy = new WorkSheet { Name = worksheet.Name, Cells =
            [.. worksheet.Cells.Select(c => new WorkCell(c.Id, c.Name, c.Formula, c.Comments))],
            Variables = new Dictionary<string, string>(worksheet.Variables, StringComparer.OrdinalIgnoreCase)
        };
        BindRecord(copy, record);
        if (additionalBindings is not null)
            foreach (var binding in additionalBindings) BindValue(copy, binding.Key, binding.Value);
        return await _interpreter.EvaluateAsync(copy, token);
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
        BindRecord(copy, record);
        if (additionalBindings is not null)
            foreach (var binding in additionalBindings) BindValue(copy, binding.Key, binding.Value);
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

    private static void BindRecord(WorkSheet worksheet, IntegrationRecord record)
    {
        // InputRecord is the stable worksheet contract. Direct field bindings remain
        // available for compatibility with existing worksheets.
        BindValue(worksheet, "InputRecord", record.Values);
        foreach (var pair in record.Values) BindValue(worksheet, pair.Key, pair.Value);
    }

    private static void BindValue(WorkSheet worksheet, string name, object? value)
    {
        if (worksheet.Variables.ContainsKey(name))
        {
            worksheet.Variables[name] = ToFormulaLiteral(value);
            return;
        }

        var cell = worksheet.Cells.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (cell is not null) cell.Formula = ToFormulaLiteral(value);
        else worksheet.Cells.Insert(0, new WorkCell(0, name, ToFormulaLiteral(value), "integration input"));
    }

    private static string ToFormulaLiteral(object? value)
    {
        if (value is null) return "Blank()";
        if (value is JsonElement json)
        {
            return json.ValueKind switch
            {
                JsonValueKind.String => JsonSerializer.Serialize(json.GetString()),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "Blank()",
                JsonValueKind.Object => ToFormulaLiteral(json.Deserialize<Dictionary<string, object?>>() ?? []),
                JsonValueKind.Array => "[" + string.Join(",", json.EnumerateArray().Select(item => ToFormulaLiteral(item))) + "]",
                _ => json.ToString()
            };
        }
        if (value is bool b) return b ? "true" : "false";
        if (value is string s) return JsonSerializer.Serialize(s);
        if (value is IDictionary dictionary) return "{" + string.Join(",", dictionary.Keys.Cast<object>().Select(k => $"{k}: {ToFormulaLiteral(dictionary[k])}")) + "}";
        if (value is IEnumerable enumerable and not byte[])
            return "[" + string.Join(",", enumerable.Cast<object?>().Select(ToFormulaLiteral)) + "]";
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "Blank()";
    }
}
