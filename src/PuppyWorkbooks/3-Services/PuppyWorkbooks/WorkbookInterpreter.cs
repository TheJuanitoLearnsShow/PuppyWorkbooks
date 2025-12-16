using System.Linq;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace PuppyWorkbooks;

public class WorkbookInterpreter
{
    
    public async IAsyncEnumerable<CellResult> ExecuteAsync(PuppyWorkbooks.WorkSheet worksheet, int uptToRow = -1, CancellationToken cancellationToken = default)
    {
        var cellsToExecute = uptToRow == -1 ?  worksheet.Cells : worksheet.Cells.Take(uptToRow);
        RecalcEngine engine = new RecalcEngine();
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
        if (result?.Type == FormulaType.)
        return result?.ToObject().ToString() ?? string.Empty;
    }

    private void OnFormulaUpdate(string arg1, FormulaValue arg2)
    {
        var output = arg2.ToObject();
        // Console.WriteLine($"{arg1}: {output}");
    }
}