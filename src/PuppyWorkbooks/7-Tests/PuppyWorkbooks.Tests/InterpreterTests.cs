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
        workbook.Cells.Add(new WorkCell(1, "Age", "10", "None"));
        workbook.Cells.Add(new WorkCell(2, "MaxLimit", "Age * 4", "None"));
        workbook.Cells.Add(new WorkCell(3, "Levels", "Table( { Value: MaxLimit }, { Value: 3 } )", "None"));
        // workbook.Cells.Add(new WorkCell(4, "NewField", "Sum(FileLines( \"SampleFiles/Input1.csv\" ) )", "None"));
        // workbook.Cells.Add(new WorkCell(5, "NewAsyncRecord", "AsyncSample( \"Here\" )", "None"));
        
        
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook))
        {
            _testOutputHelper.WriteLine(result.DisplayOutput);
            _testOutputHelper.WriteLine("____________________________");
        }
    }
}