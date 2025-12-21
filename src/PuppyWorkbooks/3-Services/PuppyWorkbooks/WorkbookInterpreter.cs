using System.Linq;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class WorkbookInterpreter
{
    private readonly PowerFxConfig _engineConfig;

    public WorkbookInterpreter()
    {
        _engineConfig = new PowerFxConfig();
        _engineConfig.AddFunction(new FileLinesFunction());
    }
    public async IAsyncEnumerable<CellResult> ExecuteAsync(PuppyWorkbooks.WorkSheet worksheet, int uptToRow = -1, CancellationToken cancellationToken = default)
    {
        var cellsToExecute = uptToRow == -1 ?  worksheet.Cells : worksheet.Cells.Take(uptToRow);
        RecalcEngine engine = new RecalcEngine(_engineConfig);
        foreach (var cell in cellsToExecute)
        {
            if (string.IsNullOrEmpty(cell.Formula)) continue;
            engine.SetFormula(cell.Name, cell.Formula, OnFormulaUpdate);
            var result = await engine.EvalAsync(cell.Name, cancellationToken);
            yield return new CellResult(ToDisplayOutput(result));
        }
    }

    private static string ToDisplayOutput(FormulaValue result)
    {
        return result.Type switch
        {
            TableType tableType => DisplayTable(result as TableValue),
            // Runtime tableType => DisplayTable(result as TableValue),
            RecordType recordType => DisplayRecord(result as RecordValue),
            _ => result?.ToObject().ToString() ?? string.Empty
        };
    }

    private static string DisplayRecord(RecordValue? result)
    {
        if (result == null) return string.Empty;
        var fields = string.Join(", ", result.Fields.Select(f => $"{f.Name}: {ToDisplayOutput(f.Value)}"));
        return fields;
    }

    private static string DisplayTable(TableValue table)
    {
        IList<string> colNames = [string.Join(", ", table.Type.FieldNames)];
        var rows = table.Rows.Select(r => ToDisplayOutput(r.Value));// string.Join(", ", r.Value)).ToList();
        return string.Join(Environment.NewLine, colNames.Concat(rows));
    }
    private void OnFormulaUpdate(string arg1, FormulaValue arg2)
    {
        var output = arg2.ToObject();
        // Console.WriteLine($"{arg1}: {output}");
    }
}