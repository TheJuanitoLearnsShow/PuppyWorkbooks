using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;

namespace PuppyWorkbooks.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public void DeserializeFile_LoadsWorksheetReferencedByRelativeFilePath()
    {
        var integrationPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration",
            "TestIntegrationWithFileReferences.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(integrationPath);

        Assert.Equal(3, definition.Steps.Count(step => step.Worksheet is not null));
        Assert.Equal("Name", definition.Steps[1].Worksheet!.Cells[0].Name);
        Assert.Equal("Keep", definition.Steps[2].Worksheet!.Cells[0].Name);
        Assert.Equal("Total", definition.Steps[3].Worksheet!.Cells[0].Name);
    }

    [Fact]
    public async Task ReferencedWorksheet_UsesItsDefaultVariablesWhenRunStandalone()
    {
        var integrationPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration",
            "TestIntegrationWithFileReferences.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(integrationPath);

        Assert.Contains("InputRecord", definition.Steps[2].Worksheet!.Variables.Keys);
        var result = await new WorkbookInterpreter().EvaluateAsync(definition.Steps[2].Worksheet!);

        Assert.Equal(true, result);
    }

    [Fact]
    public async Task CsvIntegration_MapsFiltersReducesAndWritesOneRecord()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        var outputPath = Path.Combine(directory, "output.csv");
        await File.WriteAllTextAsync(inputPath,
            "Name,Active,Amount\nAlice,true,10\nBob,false,100\nCara,true,5\n");

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "TestIntegration.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__INPUT_PATH__", inputPath, StringComparison.Ordinal)
                .Replace("__OUTPUT_PATH__", outputPath, StringComparison.Ordinal);
            var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(xmlPath));

            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(3, result.Read);
            Assert.Equal(1, result.Excluded);
            Assert.Equal(1, result.Written);
            Assert.NotNull(result.FinalState);
            Assert.Equal(15d, Convert.ToDouble(result.FinalState!["Total"]));
            Assert.DoesNotContain("Name", result.FinalState.Values.Keys);
            Assert.DoesNotContain("Amount", result.FinalState.Values.Keys);

            var lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Total", lines[0]);
            Assert.Contains("15", lines[1]);
        }
        finally
        {
            //if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Switch_RunsOnlyBranchesWhoseWorkCellIsTrue()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        await File.WriteAllTextAsync(inputPath, "Name,Amount,Active\nAlice,10,true\nBob,100,false\n");

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "Switch.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__INPUT_PATH__", inputPath, StringComparison.Ordinal);
            var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(xmlPath));

            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal("Bob-no", result.FinalState!["Result"]);
        }
        finally
        {
            //Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrationRunner_WithDebug_CapturesInputRowsAndCellsInOrderAndHierarchy()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        var outputPath = Path.Combine(directory, "output.csv");
        await File.WriteAllTextAsync(inputPath,
            "Name,Active,Amount\nAlice,true,10\nBob,false,100\nCara,true,5\n");

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "TestIntegration.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__INPUT_PATH__", inputPath, StringComparison.Ordinal)
                .Replace("__OUTPUT_PATH__", outputPath, StringComparison.Ordinal);
            var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(xmlPath));

            var runner = new IntegrationRunner(new IntegrationRunnerOptions { Debug = true });
            var result = await runner.RunAsync(definition);

            Assert.NotNull(result.DebugData);
            Assert.Equal(3, result.DebugData.Count);

            // Row 0: Alice
            var row0 = result.DebugData[0];
            Assert.Equal("Alice", row0.InputRow["Name"]);
            Assert.Equal("true", row0.InputRow["Active"]);
            Assert.Equal("10", row0.InputRow["Amount"]);
            Assert.Equal(5, row0.Steps.Count);
            Assert.Equal("input", row0.Steps[0].Id);
            Assert.Equal("IOInput", row0.Steps[0].StepType);
            Assert.Equal("map", row0.Steps[1].Id);
            Assert.Equal("Map", row0.Steps[1].StepType);
            Assert.Equal("Alice", row0.Steps[1].Cells["Name"]);
            Assert.Equal(10d, Convert.ToDouble(row0.Steps[1].Cells["Amount"]));
            Assert.Equal(true, row0.Steps[1].Cells["IsActive"]);
            Assert.Equal("filter", row0.Steps[2].Id);
            Assert.Equal("Filter", row0.Steps[2].StepType);
            Assert.Equal(true, row0.Steps[2].Cells["Keep"]);
            Assert.Equal("reduce", row0.Steps[3].Id);
            Assert.Equal("Reduce", row0.Steps[3].StepType);
            Assert.Equal(10d, Convert.ToDouble(row0.Steps[3].Cells["Total"]));
            Assert.Equal("output", row0.Steps[4].Id);
            Assert.Equal("IOOutput", row0.Steps[4].StepType);

            // Row 1: Bob (excluded by filter)
            var row1 = result.DebugData[1];
            Assert.Equal("Bob", row1.InputRow["Name"]);
            Assert.Equal("false", row1.InputRow["Active"]);
            Assert.Equal("100", row1.InputRow["Amount"]);
            Assert.Equal(3, row1.Steps.Count); // Input, Map, Filter (excluded, no Reduce/Output)
            Assert.Equal("map", row1.Steps[1].Id);
            Assert.Equal(false, row1.Steps[1].Cells["IsActive"]);
            Assert.Equal("filter", row1.Steps[2].Id);
            Assert.Equal(false, row1.Steps[2].Cells["Keep"]);

            // Row 2: Cara
            var row2 = result.DebugData[2];
            Assert.Equal("Cara", row2.InputRow["Name"]);
            Assert.Equal("true", row2.InputRow["Active"]);
            Assert.Equal("5", row2.InputRow["Amount"]);
            Assert.Equal(5, row2.Steps.Count);
            Assert.Equal(15d, Convert.ToDouble(row2.Steps[3].Cells["Total"]));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrationRunner_SwitchWithDebug_CapturesBranchHierarchyAndCellResults()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        await File.WriteAllTextAsync(inputPath, "Name,Amount,Active\nAlice,10,true\nBob,100,false\n");

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "Switch.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__INPUT_PATH__", inputPath, StringComparison.Ordinal);
            var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(xmlPath));

            var runner = new IntegrationRunner(new IntegrationRunnerOptions { Debug = true });
            var result = await runner.RunAsync(definition);

            Assert.NotNull(result.DebugData);
            Assert.Equal(2, result.DebugData.Count);

            // Row 0: Alice (Active = true -> branch IsActive executed)
            var row0 = result.DebugData[0];
            Assert.Equal("Alice", row0.InputRow["Name"]);
            Assert.Equal(2, row0.Steps.Count);
            var switchStep0 = row0.Steps[1];
            Assert.Equal("choose", switchStep0.Id);
            Assert.Equal("Switch", switchStep0.StepType);
            Assert.Equal("true", switchStep0.Cells["IsActive"]);
            Assert.Equal(false, switchStep0.Cells["IsInactive"]);
            Assert.NotNull(switchStep0.Branches);
            Assert.Equal(2, switchStep0.Branches.Count);

            var branch0_0 = switchStep0.Branches[0];
            Assert.Equal("IsActive", branch0_0.WorkCell);
            Assert.True(branch0_0.Executed);
            Assert.Single(branch0_0.Steps);
            Assert.Equal("yes", branch0_0.Steps[0].Id);
            Assert.Equal("Map", branch0_0.Steps[0].StepType);
            Assert.Equal("Alice-yes", branch0_0.Steps[0].Cells["Result"]);

            var branch0_1 = switchStep0.Branches[1];
            Assert.Equal("IsInactive", branch0_1.WorkCell);
            Assert.False(branch0_1.Executed);
            Assert.Empty(branch0_1.Steps);

            // Row 1: Bob (Active = false -> branch IsInactive executed)
            var row1 = result.DebugData[1];
            Assert.Equal("Bob", row1.InputRow["Name"]);
            var switchStep1 = row1.Steps[1];
            Assert.Equal("false", switchStep1.Cells["IsActive"]);
            Assert.Equal(true, switchStep1.Cells["IsInactive"]);
            Assert.NotNull(switchStep1.Branches);

            var branch1_0 = switchStep1.Branches[0];
            Assert.Equal("IsActive", branch1_0.WorkCell);
            Assert.False(branch1_0.Executed);
            Assert.Empty(branch1_0.Steps);

            var branch1_1 = switchStep1.Branches[1];
            Assert.Equal("IsInactive", branch1_1.WorkCell);
            Assert.True(branch1_1.Executed);
            Assert.Single(branch1_1.Steps);
            Assert.Equal("no", branch1_1.Steps[0].Id);
            Assert.Equal("Map", branch1_1.Steps[0].StepType);
            Assert.Equal("Bob-no", branch1_1.Steps[0].Cells["Result"]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrationRunner_WithInlineMockCsv_UsesMockDataWhenAllSpecified()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="Mock Data Inline Test">
                <Steps>
                    <IOInput Id="input1" Kind="SqlReader" ConnectionString="Server=invalid;Database=none;">
                        <MockCsv>
            Name,Active,Amount
            Alice,true,10
            Bob,false,100
            Cara,true,5
                        </MockCsv>
                    </IOInput>
                    <Map Id="map">
                        <Worksheet>
                            <Name>Map</Name>
                            <Cells>
                                <WorkCell>
                                    <Id>1</Id>
                                    <Name>Total</Name>
                                    <Formula>Value(InputRecord.Amount) * 2</Formula>
                                    <Comments/>
                                </WorkCell>
                            </Cells>
                        </Worksheet>
                    </Map>
                </Steps>
            </Integration>
            """;

        var definition = new IntegrationXmlSerializer().Deserialize(xml);
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "ALL"
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(3, result.Read);
        Assert.Equal(10d, Convert.ToDouble(result.FinalState!["Total"]));
    }

    [Fact]
    public async Task IntegrationRunner_WithMockCsvFilePath_UsesMockDataWhenStepIdMatches()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var mockCsvPath = Path.Combine(directory, "mock.csv");
        await File.WriteAllTextAsync(mockCsvPath, "Name,Active,Amount\nAlice,true,10\nBob,false,100\n");

        try
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Integration Name="Mock Data File Test">
                    <Steps>
                        <IOInput Id="sqlInput" Kind="SqlReader" ConnectionString="Server=invalid;" MockCsvFilePath="{mockCsvPath.Replace("\\", "/")}" />
                        <Map Id="map">
                            <Worksheet>
                                <Name>Map</Name>
                                <Cells>
                                    <WorkCell>
                                        <Id>1</Id>
                                        <Name>Greeting</Name>
                                        <Formula>"Hello " &amp; InputRecord.Name</Formula>
                                        <Comments/>
                                    </WorkCell>
                                </Cells>
                            </Worksheet>
                        </Map>
                    </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                UseMockDataForSteps = "stepA, sqlInput, stepB"
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal("Hello Bob", result.FinalState!["Greeting"]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrationRunner_WhenMockDataRequestedForStepWithoutMock_ThrowsInvalidOperationException()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="Mock Data Missing Test">
                <Steps>
                    <IOInput Id="input1" Kind="CSVReader" FilePath="input.csv" />
                </Steps>
            </Integration>
            """;

        var definition = new IntegrationXmlSerializer().Deserialize(xml);
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "input1"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(definition));
        Assert.Contains("input1", ex.Message);
        Assert.Contains("Mock data was requested", ex.Message);
    }

    [Fact]
    public async Task IntegrationRunner_WhenStepIdNotMatched_UsesActualProvider()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="Mock Not Matched Test">
                <Steps>
                    <IOInput Id="sqlInput" Kind="SqlReader" ConnectionString="Server=invalid;">
                        <MockCsv>Name,Amount&#10;Alice,10</MockCsv>
                    </IOInput>
                </Steps>
            </Integration>
            """;

        var definition = new IntegrationXmlSerializer().Deserialize(xml);
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            ConnectionFactory = null,
            UseMockDataForSteps = "otherInputStep"
        });

        // Since step ID doesn't match and ConnectionFactory is not configured, it should throw the standard SQL input exception
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(definition));
        Assert.Contains("SQL input requires ConnectionFactory", ex.Message);
    }

    [Fact]
    public void IntegrationXmlSerializer_LoadsMockDataFromVariousXmlFormats()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="Mock Formats">
                <Steps>
                    <IOInput Id="step1" Kind="SqlReader" ConnectionString="conn1">
                        <MockCsv>
            Name,Amount
            Alice,10
                        </MockCsv>
                    </IOInput>
                    <IOInput Id="step2" Kind="SqlReader" ConnectionString="conn2">
                        <MockData>
            Name,Amount
            Bob,20
                        </MockData>
                    </IOInput>
                    <IOInput Id="step3" Kind="SqlReader" ConnectionString="conn3" MockCsvFilePath="SampleFiles/Input1.csv" />
                    <IOInput Id="step4" Kind="SqlReader" ConnectionString="conn4">
                        <MockCsv FilePath="SampleFiles/Input1.csv" />
                    </IOInput>
                </Steps>
            </Integration>
            """;

        var serializer = new IntegrationXmlSerializer();
        var definition = serializer.Deserialize(xml, AppContext.BaseDirectory);

        var step1 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[0];
        Assert.Contains("Alice,10", step1.MockCsv);

        var step2 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[1];
        Assert.Contains("Bob,20", step2.MockData);

        var step3 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[2];
        Assert.True(File.Exists(step3.MockCsvFilePath));

        var step4 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[3];
        Assert.True(File.Exists(step4.MockCsvFilePath));
    }

    [Fact]
    public async Task IntegrationRunner_WithMockDataForOutputStep_SilentlyIgnoresOutputAndDoesNotWriteCsv()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var outputPath = Path.Combine(directory, "output.csv");

        try
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Integration Name="Mock Output Test">
                    <Steps>
                        <IOInput Id="input1" Kind="SqlReader" ConnectionString="Server=invalid;">
                            <MockCsv>
                Name,Amount
                Alice,10
                Bob,20
                            </MockCsv>
                        </IOInput>
                        <IOOutput Id="output1" Kind="CSVWriter" FilePath="{outputPath.Replace("\\", "/")}" />
                    </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                UseMockDataForSteps = "input1, output1"
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(0, result.Written);
            Assert.False(File.Exists(outputPath));
            Assert.Equal(true, result.FinalState!["output1.Status"]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrationRunner_WithMockDataForSqlOutput_DoesNotRequireConnectionFactoryOrConnect()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="Mock Sql Output Test">
                <Steps>
                    <IOInput Id="input1" Kind="SqlReader" ConnectionString="Server=invalid;">
                        <MockCsv>
            Name,Amount
            Alice,10
                        </MockCsv>
                    </IOInput>
                    <IOOutput Id="sqlOut" Kind="SqlWriter" ConnectionString="Server=invalid;" TableName="TargetTable" />
                </Steps>
            </Integration>
            """;

        var definition = new IntegrationXmlSerializer().Deserialize(xml);
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            ConnectionFactory = null,
            UseMockDataForSteps = "ALL"
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(1, result.Read);
        Assert.Equal(0, result.Written);
        Assert.Equal(true, result.FinalState!["sqlOut.Status"]);
    }

    [Fact]
    public async Task IntegrationRunner_WithMockDataForDeferredOutput_SilentlyIgnoresOutput()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var outputPath = Path.Combine(directory, "deferred_output.csv");

        try
        {
            var xml = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Integration Name="Mock Deferred Output Test">
                    <Steps>
                        <IOInput Id="input1" Kind="SqlReader" ConnectionString="Server=invalid;">
                            <MockCsv>
                Name,Amount
                Alice,10
                Bob,20
                            </MockCsv>
                        </IOInput>
                        <Reduce Id="reduce1" OutputField="Sum">
                            <InitialStateJson>0</InitialStateJson>
                            <Worksheet>
                                <Name>Reduce</Name>
                                <Cells>
                                    <WorkCell>
                                        <Id>1</Id>
                                        <Name>Sum</Name>
                                        <Formula>State + Value(InputRecord.Amount)</Formula>
                                        <Comments/>
                                    </WorkCell>
                                </Cells>
                            </Worksheet>
                        </Reduce>
                        <IOOutput Id="output1" Kind="CSVWriter" FilePath="{outputPath.Replace("\\", "/")}" />
                    </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                UseMockDataForSteps = "ALL"
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(0, result.Written);
            Assert.Equal(30d, Convert.ToDouble(result.FinalState!["Sum"]));
            Assert.False(File.Exists(outputPath));
            Assert.Equal(true, result.FinalState!["output1.Status"]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
