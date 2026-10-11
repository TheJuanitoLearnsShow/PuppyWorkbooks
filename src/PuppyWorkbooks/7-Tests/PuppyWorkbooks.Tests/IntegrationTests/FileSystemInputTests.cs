using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Tests;

public sealed class FileSystemInputTests
{
    [Fact]
    public async Task FileSystemInputProvider_YieldsFilesInFolder()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory,
            "SampleFiles", "Integration", "FileEnumerationTest", "TestFileEnumeration.xml");
        var xml = await File.ReadAllTextAsync(samplePath);
        var definition = new IntegrationXmlSerializer().Deserialize(xml, Path.GetDirectoryName(samplePath));

        var result = await new IntegrationRunner().RunAsync(definition);
        
        Assert.NotNull(result);
        Assert.NotNull(result.FinalState);
        // total should be 374
    }
}