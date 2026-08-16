using System.Collections;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using PuppyWorkbooks;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration;

public sealed class IntegrationRunnerOptions
{
    /// Creates a connection for SQL steps. Keeping this as a factory avoids coupling the
    /// integration library to one database vendor.
    public Func<string, DbConnection>? ConnectionFactory { get; init; }
}

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
            var reduceSteps = definition.Steps.OfType<ReduceStep>().ToList();
            var reduceStates = reduceSteps.ToDictionary(step => step, step => ParseInitialState(step.InitialStateJson));
            var outputSteps = definition.Steps.OfType<OutputStep>().ToList();
            var deferOutput = reduceSteps.Count > 0;
            long read = 0, written = 0, excluded = 0; IntegrationRecord? final = null;
            await foreach (var sourceRecord in inputProvider.ReadAsync(cancellationToken))
            {
                read++; var record = sourceRecord;
                foreach (var step in definition.Steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    switch (step)
                    {
                        case InputStep: break;
                        case MapStep map: record = await ApplyMap(map, record, cancellationToken); break;
                        case FilterStep filter:
                            if (!await ApplyFilter(filter, record, cancellationToken)) { excluded++; goto NextRecord; }
                            break;
                        case ReduceStep reduce:
                            (record, reduceStates[reduce]) = await ApplyReduce(reduce, record, reduceStates[reduce], cancellationToken);
                            break;
                        case OutputStep output when !deferOutput:
                            var status = await outputs[definition.Steps.OfType<OutputStep>().ToList().IndexOf(output)].WriteAsync(record, cancellationToken);
                            record[output.Id + ".Status"] = status.Succeeded;
                            record[output.Id + ".StatusMessage"] = status.Message;
                            record[output.Id + ".AffectedRows"] = status.AffectedRows;
                            written += status.AffectedRows;
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
            return new IntegrationResult(read, written, excluded, final);
        }
        finally { foreach (var output in outputs) await output.DisposeAsync(); }
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

    private async Task<IntegrationRecord> ApplyMap(MapStep step, IntegrationRecord record, CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        return new IntegrationRecord(values);
    }

    private async Task<bool> ApplyFilter(FilterStep step, IntegrationRecord record, CancellationToken token)
    {
        var value = await Evaluate(step.Worksheet, record, token);
        var keep = value is bool b ? b : bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
        return step.KeepWhenTrue ? keep : !keep;
    }

    private async Task<(IntegrationRecord Record, object? State)> ApplyReduce(ReduceStep step,
        IntegrationRecord record, object? state, CancellationToken token)
    {
        var next = await Evaluate(step.Worksheet, record, token,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["State"] = state });
        record[step.OutputField] = next;
        return (record, next);
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
        var copy = new WorkSheet { Name = worksheet.Name, Cells = worksheet.Cells.Select(c => new WorkCell(c.Id, c.Name, c.Formula, c.Comments)).ToList() };
        BindRecord(copy, record);
        if (additionalBindings is not null)
            foreach (var binding in additionalBindings) BindValue(copy, binding.Key, binding.Value);
        return await _interpreter.EvaluateAsync(copy, token);
    }

    private async Task<Dictionary<string, object?>> EvaluateCells(WorkSheet? worksheet,
        IntegrationRecord record, CancellationToken token)
    {
        if (worksheet is null) throw new InvalidOperationException("A worksheet is required for this step.");
        var outputCellNames = worksheet.Cells
            .Where(c => !string.IsNullOrWhiteSpace(c.Formula))
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var copy = new WorkSheet { Name = worksheet.Name, Cells = worksheet.Cells.Select(c => new WorkCell(c.Id, c.Name, c.Formula, c.Comments)).ToList() };
        BindRecord(copy, record);
        var values = await _interpreter.EvaluateCellsAsync(copy, token);
        return values
            .Where(pair => outputCellNames.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
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
