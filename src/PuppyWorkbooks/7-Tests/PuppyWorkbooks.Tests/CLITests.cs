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
    
    
    [Fact]
    public async Task Test_ReduceWorkSheet()
    {
        var worker = new WorkbooksWorker(new ExecutionSettings
        {
            WorkbookPaths = [ "SampleFiles/Integration/Reduce.xml" ]
        });
        await worker.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Test_RunIntegration_WithDebug_OutputsJsonToConsole()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        var outputPath = Path.Combine(directory, "output.csv");
        var integrationXmlPath = Path.Combine(directory, "integration.xml");
        await File.WriteAllTextAsync(inputPath, "Name,Active,Amount\nAlice,true,10\nBob,false,100\nCara,true,5\n");

        try
        {
            var xmlSourcePath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "TestIntegration.xml");
            var xml = (await File.ReadAllTextAsync(xmlSourcePath))
                .Replace("__INPUT_PATH__", inputPath.Replace("\\", "/"), StringComparison.Ordinal)
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var originalOut = Console.Out;
            using var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                var worker = new WorkbooksWorker(new ExecutionSettings
                {
                    IntegrationPath = integrationXmlPath,
                    Debug = true
                });
                await worker.StartAsync(CancellationToken.None);
                // --IntegrationPath="" --Debug="true"
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var outputText = sw.ToString();
            Assert.NotEmpty(outputText);
            Assert.Contains("\"InputRow\"", outputText);
            Assert.Contains("\"Alice\"", outputText);
            Assert.Contains("\"Steps\"", outputText);
            Assert.Contains("\"StepType\": \"Map\"", outputText);
            Assert.Contains("\"StepType\": \"Filter\"", outputText);
            Assert.Contains("\"StepType\": \"Reduce\"", outputText);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Test_RunIntegration_WithMockData_UsesMockData()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var integrationXmlPath = Path.Combine(directory, "integration.xml");
        var outputPath = Path.Combine(directory, "output.csv");

        try
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Integration Name="CLI Mock Test">
                    <Steps>
                        <IOInput Id="sqlInput" Kind="SqlReader" ConnectionString="Server=invalid;">
                            <MockCsv>
                Name,Active,Amount
                Alice,true,10
                Cara,true,5
                            </MockCsv>
                        </IOInput>
                        <Map Id="map">
                            <Worksheet>
                                <Name>Map</Name>
                                <Cells>
                                    <WorkCell>
                                        <Id>1</Id>
                                        <Name>Name</Name>
                                        <Formula>InputRecord.Name</Formula>
                                        <Comments/>
                                    </WorkCell>
                                    <WorkCell>
                                        <Id>2</Id>
                                        <Name>Amount</Name>
                                        <Formula>Value(InputRecord.Amount)</Formula>
                                        <Comments/>
                                    </WorkCell>
                                </Cells>
                            </Worksheet>
                        </Map>
                        <IOOutput Id="output" Kind="CSVWriter" FilePath="{outputPath.Replace("\\", "/")}" />
                    </Steps>
                </Integration>
                """;
            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationXmlPath,
                UseMockDataForSteps = "ALL"
            });
            await worker.StartAsync(CancellationToken.None);

            Assert.True(File.Exists(outputPath));
            var lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Equal(3, lines.Length);
            Assert.Contains("Alice", lines[1]);
            Assert.Contains("Cara", lines[2]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}