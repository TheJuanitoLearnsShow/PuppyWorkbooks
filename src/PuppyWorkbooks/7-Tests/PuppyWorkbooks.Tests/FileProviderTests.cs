using System.Text.Json;
using System.Xml.Linq;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Tests;

public sealed class FileProviderTests
{
    [Fact]
    public async Task JsonInputProvider_FromFile_ReadsObjectsFromArray()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "input.json");
            await File.WriteAllTextAsync(path, "[{\"Name\":\"Ada\",\"Amount\":10},{\"Name\":\"Bob\",\"Amount\":20}]");

            var records = await ReadAllAsync(JsonInputProvider.FromFile(path));

            Assert.Equal(2, records.Count);
            Assert.Equal("Ada", records[0]["Name"]);
            Assert.Equal(10L, records[0]["Amount"]);
            Assert.Equal("Bob", records[1]["Name"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task JsonOutputProvider_WritesRecordsAsJsonArray()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "output.json");
            await using (var provider = new JsonOutputProvider(path))
            {
                await provider.WriteAsync(CreateRecord(("Name", "Ada"), ("Amount", 10L)));
                await provider.WriteAsync(CreateRecord(("Name", "Bob"), ("Amount", 20L)));
            }

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
            Assert.Equal(2, document.RootElement.GetArrayLength());
            Assert.Equal("Ada", document.RootElement[0].GetProperty("Name").GetString());
            Assert.Equal(20L, document.RootElement[1].GetProperty("Amount").GetInt64());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task XmlInputProvider_ReadsElementsAndAttributesAsRecords()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "input.xml");
            await File.WriteAllTextAsync(path,
                "<Records><Record Id=\"1\"><Name>Ada</Name></Record><Record Id=\"2\"><Name>Bob</Name></Record></Records>");

            var records = await ReadAllAsync(new XmlInputProvider(path));

            Assert.Equal(2, records.Count);
            Assert.Equal("1", records[0]["Id"]);
            Assert.Equal("Ada", records[0]["Name"]);
            Assert.Equal("Bob", records[1]["Name"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task XmlOutputProvider_WritesRecordsAsXmlElements()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "output.xml");
            await using (var provider = new XmlOutputProvider(path))
            {
                await provider.WriteAsync(CreateRecord(("Name", "Ada"), ("Amount", 10L)));
                await provider.WriteAsync(CreateRecord(("Name", "Bob"), ("Amount", 20L)));
            }

            var document = XDocument.Load(path);
            Assert.Equal("Records", document.Root!.Name.LocalName);
            var records = document.Root.Elements("Record").ToList();
            Assert.Equal(2, records.Count);
            Assert.Equal("Ada", records[0].Element("Name")!.Value);
            Assert.Equal("20", records[1].Element("Amount")!.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IntegrationRecord CreateRecord(params (string Key, object? Value)[] values)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values) dictionary[key] = value;
        return new IntegrationRecord(dictionary);
    }

    private static async Task<List<IntegrationRecord>> ReadAllAsync(IInputProvider provider)
    {
        await using (provider)
        {
            var records = new List<IntegrationRecord>();
            await foreach (var record in provider.ReadAsync()) records.Add(record);
            return records;
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PuppyWorkbooks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
