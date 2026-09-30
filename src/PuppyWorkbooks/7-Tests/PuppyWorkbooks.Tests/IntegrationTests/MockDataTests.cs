using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;

namespace PuppyWorkbooks.Tests;

public sealed class MockDataTests
{
    [Fact]
    public async Task IntegrationRunner_WithInlineMockCsv_UsesMockDataWhenAllSpecified()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockCsvInline.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);
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
        await File.WriteAllTextAsync(mockCsvPath, await File.ReadAllTextAsync(IntegrationTests.GetSampleDataPath("MockCsvInput.csv")));

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockCsvFilePath.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__MOCK_CSV_PATH__", mockCsvPath.Replace("\\", "/"), StringComparison.Ordinal);
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
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockDataMissing.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);
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
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockNotMatched.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);
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
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockDataVariousFormats.xml");
        var xml = File.ReadAllText(xmlPath);

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
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockOutput.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
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
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockSqlOutput.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);
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
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MockDeferredOutput.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
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

    [Fact]
    public async Task IntegrationRunner_WithMultipleMockScenarios_CsvAndSql_SelectsRequestedScenario()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MultipleMockScenarios.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);

        var runnerLarge = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "input1",
            Scenario = "Large"
        });
        var resultLarge = await runnerLarge.RunAsync(definition);
        Assert.Equal(3, resultLarge.Read);

        var runnerSmall = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "input1",
            Scenario = "Small"
        });
        var resultSmall = await runnerSmall.RunAsync(definition);
        Assert.Equal(1, resultSmall.Read);
    }

    [Fact]
    public async Task IntegrationRunner_WithMultipleMockScenarios_WhenScenarioNotFound_UsesFirstMockData()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "ScenarioFallback.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);

        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            ConnectionFactory = null,
            UseMockDataForSteps = "ALL",
            Scenario = "NonExistentScenario"
        });

        var result = await runner.RunAsync(definition);
        Assert.Equal(2, result.Read);
    }

    [Fact]
    public async Task IntegrationRunner_WithHttpMockData_JsonPayload_SelectsScenarioAndParsesJson()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "HttpMockData.xml");
        var definition = new IntegrationXmlSerializer().DeserializeFile(xmlPath);

        // Run with MultiCustomer scenario
        var runnerMulti = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "httpInput",
            Scenario = "MultiCustomer"
        });
        var resultMulti = await runnerMulti.RunAsync(definition);
        Assert.Equal(3, resultMulti.Read);

        // Run with Default scenario
        var runnerDefault = new IntegrationRunner(new IntegrationRunnerOptions
        {
            UseMockDataForSteps = "httpInput"
        });
        var resultDefault = await runnerDefault.RunAsync(definition);
        Assert.Equal(1, resultDefault.Read);
    }

    [Fact]
    public async Task IntegrationRunner_WithHttpMockData_JsonFilePath_ReadsFromFile()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var jsonFile = Path.Combine(directory, "mock_response.json");
        await File.WriteAllTextAsync(jsonFile, await File.ReadAllTextAsync(IntegrationTests.GetSampleDataPath("HttpMockResponse.json")));

        try
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "HttpMockDataFile.xml");
            var xml = (await File.ReadAllTextAsync(xmlPath))
                .Replace("__JSON_FILE_PATH__", jsonFile.Replace("\\", "/"), StringComparison.Ordinal);
            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                UseMockDataForSteps = "httpInput",
                Scenario = "FromFile"
            });
            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void IntegrationXmlSerializer_LoadsMultipleMockDataSources_FromDirectAndContainerTags()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "MultipleMockDataSources.xml");
        var xml = File.ReadAllText(xmlPath);

        var serializer = new IntegrationXmlSerializer();
        var definition = serializer.Deserialize(xml, AppContext.BaseDirectory);

        var step1 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[0];
        Assert.Equal(2, step1.MockDataSources.Count);
        Assert.True(step1.MockDataSources.ContainsKey("ScenarioA"));
        Assert.True(step1.MockDataSources.ContainsKey("ScenarioB"));
        Assert.Contains("Alice,10", step1.MockDataSources["ScenarioA"].Content);
        Assert.True(File.Exists(step1.MockDataSources["ScenarioB"].FilePath));

        var step2 = (PuppyWorkbooks.Integration.Models.InputStep)definition.Steps[1];
        Assert.Equal(2, step2.MockDataSources.Count);
        Assert.True(step2.MockDataSources.ContainsKey("Scenario1"));
        Assert.True(step2.MockDataSources.ContainsKey("Scenario2"));
    }
}