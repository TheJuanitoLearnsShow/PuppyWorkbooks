using Xunit.Abstractions;

namespace PuppyWorkbooks.Tests;

public class InterpreterTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public InterpreterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Test_Interpretation()
    {
        var workbook = new WorkSheet();
        // workbook.Cells.Add(new WorkCell(1, "Ages", "[1,2,3]", "None"));
        // workbook.Cells.Add(new WorkCell(1, "Age", "Sum(Ages, Value * Value)", "None"));
        // workbook.Cells.Add(new WorkCell(2, "MaxLimit", "Age * 4", "None"));
        // workbook.Cells.Add(new WorkCell(3, "Levels", "Table( { Level: MaxLimit }, { Level: 3 } )", "None"));
        // workbook.Cells.Add(new WorkCell(5, "NewField", "Sum(Levels As t, t.Level )", ""));
        workbook.Cells.Add(new WorkCell(4, "SampleNumberCollection", "FileLines( \"SampleFiles/Input1.csv\" )", "None"));
        workbook.Cells.Add(new WorkCell(5, "SumOfFileLines", "Sum(SampleNumberCollection As t, AddTax(t.Price) )", "For now, Price is the column name created in FileLinesFunction"));
        // workbook.Cells.Add(new WorkCell(6, "NewAsyncRecord", "AsyncSample( \"Here\" )", "None"));
        
        
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: false))
        {
            _testOutputHelper.WriteLine(result.DisplayOutput);
            _testOutputHelper.WriteLine("____________________________");
        }
    }
}