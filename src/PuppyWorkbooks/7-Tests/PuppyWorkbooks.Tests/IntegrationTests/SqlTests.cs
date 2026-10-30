using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;

namespace PuppyWorkbooks.Tests.IntegrationTests;

public sealed class SqlTests
{
    [Fact]
    public async Task SqlOutputProvider_ExecutesStoredProcedure()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory,
            "SampleFiles", "Integration", "SqlTests", "TestSqlOutput.xml");
        var xml = await File.ReadAllTextAsync(samplePath);
        var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(samplePath));

        var result = await new IntegrationRunner().RunAsync(definition);
        
        Assert.NotNull(result);
        Assert.NotNull(result.FinalState);
        // total should be 374
    }
}