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

    [Fact]
    public void DeserializeFromXmlFile_ReadsTestScenarioInputsAndExpectedCellValues()
    {
        var worksheet = _workSheetSerializer.DeserializeFromXmlFile("SampleFiles/TestScenarioWorkbook.xml");

        var scenario = Assert.Single(worksheet.TestScenarios);
        Assert.Equal("TripleMultiplier", scenario.Name);
        var input = Assert.Single(scenario.Inputs);
        Assert.Equal("Multiplier", input.Key);
        Assert.Equal("3", input.Value);
        var expectedCell = Assert.Single(scenario.ExpectedCells);
        Assert.Equal("Total", expectedCell.Name);
        Assert.Equal("15", expectedCell.Value);
    }

    [Fact]
    public async Task ExecuteTestScenariosAsync_UsesScenarioInputsAndReportsExpectedCellValues()
    {
        var worksheet = _workSheetSerializer.DeserializeFromXmlFile("SampleFiles/TestScenarioWorkbook.xml");

        var result = Assert.Single(await new WorkbookInterpreter().ExecuteTestScenariosAsync(worksheet,
            scenarioName: "triplemultiplier"));

        Assert.Equal("TripleMultiplier", result.Name);
        Assert.True(result.Passed);
        var expectedCell = Assert.Single(result.ExpectedCells);
        Assert.Equal("Total", expectedCell.CellName);
        Assert.Equal("15", expectedCell.ActualValue);
        Assert.True(expectedCell.Passed);
        Assert.Equal("2", worksheet.Variables["Multiplier"]);
    }
}
