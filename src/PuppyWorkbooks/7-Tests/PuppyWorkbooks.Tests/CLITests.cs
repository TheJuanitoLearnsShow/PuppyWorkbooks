using PuppyWorkbooks.CLI;
using PuppyWorkbooks.Serialization;
using Xunit.Abstractions;

namespace PuppyWorkbooks.Tests;

public class CLITests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly WorkSheetSerializer _workSheetSerializer = new();

    public CLITests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Test_RunWorkbook()
    {
        var worker = new WorkbooksWorker(new ExecutionSettings
        {
            WorkbookPaths = [ "SampleFiles/TestWorkbook.xml" ],
            InputData = new Dictionary<string, string>
            {
                { "TestAgeInput", "50" }
            }
        });
        await worker.StartAsync(CancellationToken.None);
    }
}