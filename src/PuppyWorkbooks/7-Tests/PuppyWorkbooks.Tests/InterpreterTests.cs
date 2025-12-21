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
        var workbook = new PuppyWorkbooks.WorkSheet();
        workbook.Cells.Add(new PuppyWorkbooks.WorkCell(1, "Age", "10", "None"));
        workbook.Cells.Add(new PuppyWorkbooks.WorkCell(2, "MaxLimit", "Age * 4", "None"));
        workbook.Cells.Add(new PuppyWorkbooks.WorkCell(3, "Levels", "Table( { Value: MaxLimit }, { Value: 3 } )", "None"));
        workbook.Cells.Add(new PuppyWorkbooks.WorkCell(4, "NewField", "FileLines( \"Hi\" )", "None"));
        
        var interpreter = new PuppyWorkbooks.WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook))
        {
            _testOutputHelper.WriteLine(result.DisplayOutput);
        }
    }
}