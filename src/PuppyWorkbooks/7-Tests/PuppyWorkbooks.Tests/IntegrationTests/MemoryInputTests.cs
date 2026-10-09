using Microsoft.PowerFx.Types;
using PuppyWorkbooks.CLI;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;
using Xunit;

namespace PuppyWorkbooks.Tests;

public sealed class MemoryInputTests
{
    private class SampleOrder
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromJson_Array()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = "[{\"Id\": 1, \"Amount\": 100}, {\"Id\": 2, \"Amount\": 200}]";
            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner( new IntegrationRunnerOptions()
            {
                InputData = json
            });
            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(2, result.Written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromJson_SingleObject()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = "{\"Id\": 42, \"Amount\": 500}";
            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner( new IntegrationRunnerOptions()
            {
                InputData = json
            });
            var result = await runner.RunAsync(definition);

            Assert.Equal(1, result.Read);
            Assert.Equal(1, result.Written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromCSharpObject_ListAndSingle()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var orders = new List<SampleOrder>
            {
                new() { Id = 1, Amount = 150m },
                new() { Id = 2, Amount = 350m }
            };

            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader, Data = orders },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner();
            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(2, result.Written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromCSharpObject_AnonymousType()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var anonymous = new { Id = 99, Name = "SpecialOrder", Amount = 999m };
            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader, Data = anonymous },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner();
            var result = await runner.RunAsync(definition);

            Assert.Equal(1, result.Read);
            Assert.Equal(1, result.Written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromNativePowerFxRecord()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var powerFxRecord1 = FormulaValue.NewRecordFromFields(
                new NamedValue("Id", FormulaValue.New(1)),
                new NamedValue("Amount", FormulaValue.New(100m)));

            var powerFxRecord2 = FormulaValue.NewRecordFromFields(
                new NamedValue("Id", FormulaValue.New(2)),
                new NamedValue("Amount", FormulaValue.New(200m)));

            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader, Data = new[] { powerFxRecord1, powerFxRecord2 } },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner();
            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(2, result.Written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MemoryInputProvider_ToPowerFxRecords_TransformsJsonAndObjectsToRecordValues()
    {
        // 1. JSON string transforms to RecordValue
        var json = "{\"Name\": \"Puppy\", \"Count\": 3}";
        var recordsFromJson = MemoryInputProvider.ToPowerFxRecords(json).ToList();
        Assert.Single(recordsFromJson);
        Assert.IsAssignableFrom<RecordValue>(recordsFromJson[0]);
        Assert.Equal("Puppy", recordsFromJson[0].GetField("Name").ToObject());

        // 2. C# object transforms to RecordValue
        var poco = new SampleOrder { Id = 10, Amount = 25.5m };
        var recordsFromPoco = MemoryInputProvider.ToPowerFxRecords(poco).ToList();
        Assert.Single(recordsFromPoco);
        Assert.IsAssignableFrom<RecordValue>(recordsFromPoco[0]);
        Assert.Equal(10L, Convert.ToInt64(recordsFromPoco[0].GetField("Id").ToObject()));

        // 3. Native Power Fx record is preserved as RecordValue
        var nativeRecord = FormulaValue.NewRecordFromFields(new NamedValue("Key", FormulaValue.New("Value123")));
        var recordsFromNative = MemoryInputProvider.ToPowerFxRecords(nativeRecord).ToList();
        Assert.Single(recordsFromNative);
        Assert.Same(nativeRecord, recordsFromNative[0]);
    }

    [Fact]
    public async Task MemoryInput_WorksWithDownstreamMapAndFilterSteps()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var data = new[]
            {
                new { Item = "A", Price = 10 },
                new { Item = "B", Price = 50 },
                new { Item = "C", Price = 5 }
            };

            var definition = new IntegrationDefinition
            {
                Name = "Test",
                Steps = new List<IntegrationStep>
                {
                    new InputStep { Id = "source", Kind = InputKind.MemoryReader, Data = data },
                    new FilterStep
                    {
                        Id = "filter",
                        KeepWhenTrue = true,
                        Worksheet = new WorkSheet
                        {
                            Name = "FilterSheet",
                            Cells = new List<WorkCell>
                            {
                                new(1, "Keep", "Value(InputRecord.Price) > 8", "")
                            }
                        }
                    },
                    new MapStep
                    {
                        Id = "map",
                        Worksheet = new WorkSheet
                        {
                            Name = "MapSheet",
                            Cells = new List<WorkCell>
                            {
                                new(1, "Item", "InputRecord.Item", ""),
                                new(2, "TotalPrice", "Value(InputRecord.Price) * 1.1", "")
                            }
                        }
                    },
                    new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = tempFile }
                }
            };

            var runner = new IntegrationRunner();
            var result = await runner.RunAsync(definition);

            Assert.Equal(3, result.Read);
            Assert.Equal(2, result.Written); // Item A (10) and B (50) kept, C (5) excluded
            Assert.Equal(1, result.Excluded);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CLI_LoadInputValues_And_ExecuteIntegration_WithMemoryInput()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputJsonFile = Path.Combine(directory, "input.json");
        var outputJsonFile = Path.Combine(directory, "output.json");
        var integrationXmlPath = Path.Combine(directory, "integration.xml");

        await File.WriteAllTextAsync(inputJsonFile, "{\"FirstName\": \"Bob\", \"Score\": \"100\"}");

        var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Integration Name=""CliMemoryTest"">
  <Steps>
    <IOInput Id=""memInput"" Kind=""MemoryReader"" />
    <IOOutput Id=""outSink"" Kind=""JsonWriter"" FilePath=""{outputJsonFile.Replace("\\", "/")}"" />
  </Steps>
</Integration>";
        await File.WriteAllTextAsync(integrationXmlPath, xml);

        try
        {
            var worker = new WorkbooksWorker(new ExecutionSettings
            {
                IntegrationPath = integrationXmlPath,
                InputDataPath = inputJsonFile
            });

            await worker.StartAsync(CancellationToken.None);

            Assert.True(File.Exists(outputJsonFile));
            var outputContent = await File.ReadAllTextAsync(outputJsonFile);
            Assert.Contains("Bob", outputContent);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
