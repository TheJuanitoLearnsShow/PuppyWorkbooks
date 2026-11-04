using PuppyWorkbooks.CLI;
using PuppyWorkbooks.Serialization;
using Xunit.Abstractions;

namespace PuppyWorkbooks.Tests;

[Collection("ConsoleTests")]
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
        var sampleInputPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "SampleData", "StandardInput.csv");
        await File.WriteAllTextAsync(inputPath, await File.ReadAllTextAsync(sampleInputPath));

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
    public async Task Test_GenerateIntegrationHtmlReport_IncludesNestedIntegrationAndRedactsSecrets()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var integrationPath = Path.Combine(directory, "parent.xml");
        var childPath = Path.Combine(directory, "child.xml");
        var worksheetPath = Path.Combine(directory, "worksheet.xml");
        var reportPath = Path.Combine(directory, "reports", "audit.html");

        try
        {
            await File.WriteAllTextAsync(worksheetPath, """
                                                    <WorkSheet>
                                                      <Cells>
                                                        <WorkCell><Id>1</Id><Name>LessThan</Name><Formula>1 &lt; 2</Formula></WorkCell>
                                                      </Cells>
                                                    </WorkSheet>
                                                    """);
            await File.WriteAllTextAsync(childPath, """
                                                    <Integration Name="Child Integration">
                                                      <Steps>
                                                        <MemoryInput Id="source" />
                                                        <MemoryOutput Id="sink" />
                                                      </Steps>
                                                    </Integration>
                                                    """);
            await File.WriteAllTextAsync(integrationPath, """
                                                          <Integration Name="Parent Integration">
                                                            <HttpConfigurations>
                                                              <HttpConfiguration Name="service" ConnectionString="Server=secret-db;" />
                                                            </HttpConfigurations>
                                                            <Steps>
                                                              <Map Id="mapping">
                                                                <Worksheet FilePath="worksheet.xml" />
                                                              </Map>
                                                              <RunIntegration Id="child" FilePath="child.xml" FromField="Items" />
                                                            </Steps>
                                                          </Integration>
                                                          """);

            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationPath,
                HtmlReportPath = reportPath
            });
            await worker.StartAsync(CancellationToken.None);

            var report = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("Parent Integration", report);
            Assert.Contains("Child Integration", report);
            Assert.Contains("href=\"#integration-2\"", report);
            Assert.Contains("id=\"integration-2\"", report);
            Assert.Contains("FromField", report);
            Assert.Contains("Referenced worksheet", report);
            Assert.Contains("worksheet.xml", report);
            Assert.Contains("1 &amp;lt; 2", report);
            Assert.Contains("[REDACTED]", report);
            Assert.DoesNotContain("secret-db", report);
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
            var xmlSourcePath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "CliMockData.xml");
            var xml = (await File.ReadAllTextAsync(xmlSourcePath))
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationXmlPath,
                UseMockDataForSteps = "sqlInput"
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

    [Fact]
    public async Task Test_RunIntegration_WithMockDataAll_DoesNotWriteOutputFile()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var integrationXmlPath = Path.Combine(directory, "integration.xml");
        var outputPath = Path.Combine(directory, "output.csv");

        try
        {
            var xmlSourcePath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "CliMockDataAll.xml");
            var xml = (await File.ReadAllTextAsync(xmlSourcePath))
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationXmlPath,
                UseMockDataForSteps = "ALL"
            });
            await worker.StartAsync(CancellationToken.None);

            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Test_RunIntegration_WithScenario_UsesSpecifiedScenarioMockData()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var integrationXmlPath = Path.Combine(directory, "integration.xml");
        var outputPath = Path.Combine(directory, "output.csv");

        try
        {
            var xmlSourcePath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "CliScenario.xml");
            var xml = (await File.ReadAllTextAsync(xmlSourcePath))
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationXmlPath,
                UseMockDataForSteps = "sqlInput",
                Scenario = "ScenarioTwo"
            });
            await worker.StartAsync(CancellationToken.None);

            Assert.True(File.Exists(outputPath));
            var lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Equal(3, lines.Length);
            Assert.Contains("Bob", lines[1]);
            Assert.Contains("Cara", lines[2]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}