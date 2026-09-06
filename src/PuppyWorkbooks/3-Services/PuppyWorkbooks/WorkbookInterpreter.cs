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
        _engineConfig.AddFunction(new AddTaxFunction());
    }
    public async IAsyncEnumerable<CellResult> ExecuteAsync(WorkSheet worksheet, int uptToRow = -1, bool yieldResultsForEachCell = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var engine = new RecalcEngine(_engineConfig);
        EvaluateAndUpdateVariables(worksheet, engine);
        var cellsToExecute = GetExecutionCells(worksheet, uptToRow);
        foreach (var cell in cellsToExecute)
        {
            if (string.IsNullOrEmpty(cell.Formula)) continue;
            if (CanBeUsedAsFormula(cell))
            {
                EvaluateFormula(worksheet, engine, cell);
                // For future decision, should the cells be allowed to contain variables? Having immutability helps with cognitive load.
            }
            if (!yieldResultsForEachCell && cell != cellsToExecute.Last()) continue;
            var result = await engine.EvalAsync(cell.Name, cancellationToken);
            yield return new CellResult(ValueFormatter.ToDisplayOutput(result), cell.Id, cell.Name);
        }
    }

    /// Evaluates the final cell and returns its native Power Fx value. This is useful to
    /// services which need to use a worksheet as a function rather than display its result.
    public async Task<object?> EvaluateAsync(WorkSheet worksheet, CancellationToken cancellationToken = default)
    {
        var engine = new RecalcEngine(_engineConfig);
        
        EvaluateCells(worksheet, engine);
        var last = worksheet.Cells.LastOrDefault(c => !string.IsNullOrWhiteSpace(c.Formula));
        if (last is null) return null;
        var result = await engine.EvalAsync(last.Name, cancellationToken);
        return result.ToObject();
    }

    private void EvaluateCells(WorkSheet worksheet, RecalcEngine engine)
    {
        EvaluateAndUpdateVariables(worksheet, engine);
        var cellsToExecute = GetExecutionCells(worksheet).Where(c => !string.IsNullOrWhiteSpace(c.Formula));
        foreach (var cell in cellsToExecute)
        {
            EvaluateFormula(worksheet, engine, cell);
        }
    }

    private void EvaluateFormula(WorkSheet worksheet, RecalcEngine engine, WorkCell cell)
    {
        try
        {
            engine.SetFormula(cell.Name, cell.Formula, OnFormulaUpdate);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error setting formula for cell '{cell.Name}' in {worksheet.Name}: {ex.Message}", ex);
        }
    }

    private static void EvaluateAndUpdateVariables(WorkSheet worksheet, RecalcEngine engine)
    {
        foreach (var worksheetInputVariable in worksheet.Variables)
        {
            var powerFxValue = engine.Eval(worksheetInputVariable.Value);
            engine.UpdateVariable(worksheetInputVariable.Key, powerFxValue);
        }
    }

    /// Evaluates every formula cell and returns the native value of each cell keyed by
    /// its name. This is used by integration map steps, where a worksheet is a record
    /// projection rather than a single scalar expression.
    public async Task<Dictionary<string, object?>> EvaluateCellsAsync(WorkSheet worksheet,
        CancellationToken cancellationToken = default)
    {
        var engine = new RecalcEngine(_engineConfig);

        EvaluateCells(worksheet, engine);

        var results = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in worksheet.Cells.Where(c => !string.IsNullOrWhiteSpace(c.Formula)))
        {
            var result = await engine.EvalAsync(cell.Name, cancellationToken);
            results[cell.Name] = result.ToObject();
        }

        return results;
    }

    private static List<WorkCell> GetExecutionCells(WorkSheet worksheet, int uptToRow = -1)
    {
        var cells = uptToRow == -1 ? worksheet.Cells : worksheet.Cells.Take(uptToRow);
        return [.. cells];
    }

    private static bool CanBeUsedAsFormula(WorkCell cell)
    {
        return !string.IsNullOrEmpty(cell.Formula);
    }

    private void OnFormulaUpdate(string arg1, FormulaValue arg2)
    {
        var output = arg2.ToObject();
        // Console.WriteLine($"{arg1}: {output}");
    }
}
