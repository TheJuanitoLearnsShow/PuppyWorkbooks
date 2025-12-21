using System.Linq;
using System.Runtime.CompilerServices;
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
        _engineConfig.AddFunction(new AsyncSampleFunction());
    }
    public async IAsyncEnumerable<CellResult> ExecuteAsync(WorkSheet worksheet, int uptToRow = -1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cellsToExecute = uptToRow == -1 ?  worksheet.Cells : worksheet.Cells.Take(uptToRow);
        var engine = new RecalcEngine(_engineConfig);
        foreach (var cell in cellsToExecute)
        {
            if (string.IsNullOrEmpty(cell.Formula)) continue;
            engine.SetFormula(cell.Name, cell.Formula, OnFormulaUpdate);
            var result = await engine.EvalAsync(cell.Name, cancellationToken);
            yield return new CellResult(ValueFormatter.ToDisplayOutput(result));
        }
    }

    private void OnFormulaUpdate(string arg1, FormulaValue arg2)
    {
        var output = arg2.ToObject();
        // Console.WriteLine($"{arg1}: {output}");
    }
}