using System.Text.Json;
using PuppyWorkbooks.CLI;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;
using Xunit;

namespace PuppyWorkbooks.Tests;

[Collection("ConsoleTests")]
public sealed class MemoryOutputTests
{
    private class TestOrder
    {
        public int Id { get; set; }
        public string Item { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    [Fact]
    public async Task MemoryOutputProvider_DirectCall_InvokesAction()
    {
        var records = new List<IntegrationRecord>();
        var provider = new MemoryOutputProvider(record => records.Add(record));

        var inputRecord = new IntegrationRecord(new Dictionary<string, object?>
        {
            ["Id"] = 1,
            ["Name"] = "Widget"
        });

        var status = await provider.WriteAsync(inputRecord);

        Assert.True(status.Succeeded);
        Assert.Equal(1, status.AffectedRows);
        Assert.Single(records);
        Assert.Equal(1, records[0]["Id"]);
        Assert.Equal("Widget", records[0]["Name"]);
    }

    [Fact]
    public async Task MemoryOutputProvider_WithNullAction_SucceedsWithoutException()
    {
        var provider = new MemoryOutputProvider();
        var inputRecord = new IntegrationRecord(new Dictionary<string, object?>
        {
            ["Id"] = 1
        });

        var status = await provider.WriteAsync(inputRecord);

        Assert.True(status.Succeeded);
        Assert.Equal(1, status.AffectedRows);
    }

    [Fact]
    public async Task IntegrationRunner_ProgrammaticExecution_YieldsRowsToAction()
    {
        var yieldedRecords = new List<IntegrationRecord>();
        var orders = new List<TestOrder>
        {
            new() { Id = 101, Item = "Keyboard", Price = 49.99m },
            new() { Id = 102, Item = "Mouse", Price = 25.50m }
        };

        var definition = new IntegrationDefinition
        {
            Name = "ProgrammaticMemoryTest",
            Steps = new List<IntegrationStep>
            {
                new MemoryInputStep { Id = "source", Data = orders },
                new MemoryOutputStep { Id = "sink" }
            }
        };

        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            Action = record => yieldedRecords.Add(record)
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(2, result.Read);
        Assert.Equal(2, yieldedRecords.Count);
        Assert.Equal(101, Convert.ToInt32(yieldedRecords[0]["Id"]));
        Assert.Equal("Keyboard", yieldedRecords[0]["Item"]);
        Assert.Equal(102, Convert.ToInt32(yieldedRecords[1]["Id"]));
        Assert.Equal("Mouse", yieldedRecords[1]["Item"]);
    }

    [Fact]
    public async Task IntegrationRunner_XmlDeserialization_MemoryOutputElement_YieldsRows()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="MemoryOutputXmlTest">
                <Steps>
                    <MemoryInput Id="source" />
                    <MemoryOutput Id="sink" />
                </Steps>
            </Integration>
            """;

        var serializer = new IntegrationXmlSerializer();
        var definition = serializer.Deserialize(xml);

        var yieldedRecords = new List<IntegrationRecord>();
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            InputData = "[{\"Id\": 1, \"Title\": \"First\"}, {\"Id\": 2, \"Title\": \"Second\"}]",
            Action = record => yieldedRecords.Add(record)
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(2, result.Read);
        Assert.Equal(2, yieldedRecords.Count);
        Assert.Equal(1, Convert.ToInt32(yieldedRecords[0]["Id"]));
        Assert.Equal("First", yieldedRecords[0]["Title"]);
    }

    [Fact]
    public async Task IntegrationRunner_XmlDeserialization_IOOutputKindMemoryWriter_YieldsRows()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="IOMemoryWriterXmlTest">
                <Steps>
                    <MemoryInput Id="source" />
                    <IOOutput Id="sink" Kind="MemoryWriter" />
                </Steps>
            </Integration>
            """;

        var serializer = new IntegrationXmlSerializer();
        var definition = serializer.Deserialize(xml);

        var yieldedRecords = new List<IntegrationRecord>();
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            InputData = "[{\"Name\": \"Alpha\"}, {\"Name\": \"Beta\"}]",
            OutputAction = record => yieldedRecords.Add(record)
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(2, result.Read);
        Assert.Equal(2, yieldedRecords.Count);
        Assert.Equal("Alpha", yieldedRecords[0]["Name"]);
        Assert.Equal("Beta", yieldedRecords[1]["Name"]);
    }

    [Fact]
    public async Task CLI_Execution_WithMemoryOutput_OutputsRowsToConsole()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var integrationXmlPath = Path.Combine(directory, "integration.xml");

        try
        {
            var xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <Integration Name="CliMemoryOutputTest">
                    <Steps>
                        <SqlInput Id="sqlInput" ConnectionString="Server=invalid;">
                            <MockCsv>
                Id,Name
                10,Gadget
                20,Gizmo
                            </MockCsv>
                        </SqlInput>
                        <MemoryOutput Id="sink" />
                    </Steps>
                </Integration>
                """;

            await File.WriteAllTextAsync(integrationXmlPath, xml);

            var originalOut = Console.Out;
            using var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                var worker = new WorkbooksWorker(new ExecutionSettings
                {
                    IntegrationPath = integrationXmlPath,
                    UseMockDataForSteps = "sqlInput"
                });
                await worker.StartAsync(CancellationToken.None);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var outputText = sw.ToString();
            Assert.NotEmpty(outputText);
            Assert.Contains("Gadget", outputText);
            Assert.Contains("Gizmo", outputText);
            Assert.Contains("\"Id\": \"10\"", outputText);
            Assert.Contains("\"Id\": \"20\"", outputText);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WorkbooksWorker_OutputRecord_OutputsFormattedJson()
    {
        var record = new IntegrationRecord(new Dictionary<string, object?>
        {
            ["Key"] = "Value123",
            ["Num"] = 42
        });

        var originalOut = Console.Out;
        using var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            WorkbooksWorker.OutputRecord(record);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var outputText = sw.ToString();
        Assert.Contains("\"Key\": \"Value123\"", outputText);
        Assert.Contains("\"Num\": 42", outputText);
    }

    [Fact]
    public void MemoryOutputStep_RoundTripSerialization()
    {
        var serializer = new IntegrationXmlSerializer();
        var definition = new IntegrationDefinition
        {
            Name = "RoundTripMemory",
            Steps = new List<IntegrationStep>
            {
                new MemoryInputStep { Id = "in" },
                new MemoryOutputStep { Id = "out" }
            }
        };

        var xml = serializer.Serialize(definition);
        var roundTripped = serializer.Deserialize(xml);

        var outputStep = Assert.IsType<MemoryOutputStep>(roundTripped.Steps[1]);
        Assert.Equal("out", outputStep.Id);
        Assert.Equal(OutputKind.MemoryWriter, outputStep.Kind);
    }
}
