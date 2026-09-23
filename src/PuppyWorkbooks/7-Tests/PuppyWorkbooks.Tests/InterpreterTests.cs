using PuppyWorkbooks.Serialization;
using Xunit.Abstractions;

namespace PuppyWorkbooks.Tests;

public class InterpreterTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly WorkSheetSerializer _workSheetSerializer = new ();

    public InterpreterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Test_Interpretation()
    {
        var workbook = _workSheetSerializer.DeserializeFromXmlFile("SampleFiles/InterpreterSample.xml");
        
        
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: false))
        {
            _testOutputHelper.WriteLine(result.DisplayOutput);
            _testOutputHelper.WriteLine("____________________________");
        }
        _workSheetSerializer.SerializeToXmlFile("TestWorkbook.xml", workbook);
    }
}