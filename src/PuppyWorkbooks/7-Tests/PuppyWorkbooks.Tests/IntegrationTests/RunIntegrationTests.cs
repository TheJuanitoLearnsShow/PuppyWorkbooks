using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Tests.IntegrationTests;

public sealed class RunIntegrationTests
{
    [Fact]
    public async Task RunIntegration_PassesCurrentRowToChildMemoryInput()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "child.xml"), """
                <Integration Name="Child">
                  <Steps>
                    <MemoryInput Id="source" />
                    <MemoryOutput Id="sink" />
                  </Steps>
                </Integration>
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "parent.xml"), """
                <Integration Name="Parent">
                  <Steps>
                    <MemoryInput Id="source" />
                    <RunIntegration Id="child" FilePath="child.xml" />
                  </Steps>
                </Integration>
                """);

            var childRows = new List<IntegrationRecord>();
            var definition = new IntegrationXmlSerializer().DeserializeFile(Path.Combine(directory, "parent.xml"));
            var result = await new IntegrationRunner(new IntegrationRunnerOptions
            {
                InputData = new { Id = 7, Name = "Puppy" },
                Action = childRows.Add
            }).RunAsync(definition);

            Assert.Equal(1, result.Read);
            var row = Assert.Single(childRows);
            Assert.Equal(7L, Convert.ToInt64(row["Id"]));
            Assert.Equal("Puppy", row["Name"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunIntegration_PassesPowerFxTableFromSelectedFieldToChildMemoryInput()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "child.xml"), """
                <Integration Name="Child">
                  <Steps>
                    <MemoryInput Id="source" />
                    <MemoryOutput Id="sink" />
                  </Steps>
                </Integration>
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "parent.xml"), """
                <Integration Name="Parent">
                  <Steps>
                    <MemoryInput Id="source" />
                    <Map Id="createTable">
                      <Worksheet>
                        <Cells>
                          <WorkCell><Id>1</Id><Name>Items</Name><Formula>Table({ChildId: 1}, {ChildId: 2})</Formula></WorkCell>
                        </Cells>
                      </Worksheet>
                    </Map>
                    <RunIntegration Id="child" FilePath="child.xml" FromField="Items" />
                  </Steps>
                </Integration>
                """);

            var childRows = new List<IntegrationRecord>();
            var definition = new IntegrationXmlSerializer().DeserializeFile(Path.Combine(directory, "parent.xml"));
            await new IntegrationRunner(new IntegrationRunnerOptions
            {
                InputData = new { Trigger = true },
                Action = childRows.Add
            }).RunAsync(definition);

            Assert.Equal(2, childRows.Count);
            Assert.Equal(1L, Convert.ToInt64(childRows[0]["ChildId"]));
            Assert.Equal(2L, Convert.ToInt64(childRows[1]["ChildId"]));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunIntegration_IgnoresPassedDataWhenChildHasNoMemoryInput()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var childDataPath = Path.Combine(directory, "child-data.json").Replace('\\', '/');
            await File.WriteAllTextAsync(childDataPath, "[{\"ChildId\": 99}]");
            await File.WriteAllTextAsync(Path.Combine(directory, "child.xml"), $"""
                <Integration Name="Child">
                  <Steps>
                    <JsonInput Id="source" FilePath="{childDataPath}" />
                    <MemoryOutput Id="sink" />
                  </Steps>
                </Integration>
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "parent.xml"), """
                <Integration Name="Parent">
                  <Steps>
                    <MemoryInput Id="source" />
                    <RunIntegration Id="child" FilePath="child.xml" />
                  </Steps>
                </Integration>
                """);

            var childRows = new List<IntegrationRecord>();
            var definition = new IntegrationXmlSerializer().DeserializeFile(Path.Combine(directory, "parent.xml"));
            await new IntegrationRunner(new IntegrationRunnerOptions
            {
                InputData = new { Id = 7 },
                Action = childRows.Add
            }).RunAsync(definition);

            var row = Assert.Single(childRows);
            Assert.Equal(99L, Convert.ToInt64(row["ChildId"]));
            Assert.Null(row["Id"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
