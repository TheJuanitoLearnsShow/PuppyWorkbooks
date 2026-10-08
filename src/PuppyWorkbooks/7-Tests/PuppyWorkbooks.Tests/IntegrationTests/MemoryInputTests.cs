using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using Xunit;

namespace PuppyWorkbooks.Tests;

public sealed class MemoryInputTests
{
    [Fact]
    public async Task MemoryInputProvider_YieldsRecordsFromJson()
    {
        var json = "[{\"Id\": 1, \"Amount\": 100}, {\"Id\": 2, \"Amount\": 200}]";
        var definition = new IntegrationDefinition
        {
            Name = "Test",
            Steps = new List<IntegrationStep>
            {
                new InputStep { Id = "source", Kind = InputKind.MemoryReader, MemoryData = json },
                new OutputStep { Id = "sink", Kind = OutputKind.JsonWriter, FilePath = "test.json" }
            }
        };
        
        var runner = new IntegrationRunner();
        var result = await runner.RunAsync(definition);
        
        Assert.Equal(2, result.Read);
    }
}
