using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public async Task CsvIntegration_MapsFiltersReducesAndWritesOneRecord()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PuppyWorkbooks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "input.csv");
        var outputPath = Path.Combine(directory, "output.csv");
        await File.WriteAllTextAsync(inputPath,
            "Name,Active,Amount\nAlice,true,10\nBob,false,100\nCara,true,5\n");

        try
        {
            var definition = new IntegrationDefinition
            {
                Name = "Sales total",
                Steps =
                [
                    new InputStep { Id = "input", Kind = InputKind.CSVReader, FilePath = inputPath },
                    new MapStep
                    {
                        Id = "map",
                        Worksheet = new WorkSheet
                        {
                            Cells =
                            [
                                new WorkCell(1, "Name", "InputRecord.Name", ""),
                                new WorkCell(2, "Amount", "Value(InputRecord.Amount)", ""),
                                new WorkCell(3, "IsActive", "InputRecord.Active = \"true\"", "")
                            ]
                        }
                    },
                    new FilterStep
                    {
                        Id = "filter",
                        Worksheet = new WorkSheet
                        {
                            Cells = [new WorkCell(1, "Keep", "InputRecord.IsActive", "")]
                        }
                    },
                    new ReduceStep
                    {
                        Id = "reduce",
                        OutputField = "Total",
                        InitialStateJson = "0",
                        Worksheet = new WorkSheet
                        {
                            Cells = [new WorkCell(1, "Total", "State + InputRecord.Amount", "")]
                        }
                    },
                    new OutputStep { Id = "output", Kind = OutputKind.CSVWriter, FilePath = outputPath }
                ]
            };

            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(3, result.Read);
            Assert.Equal(1, result.Excluded);
            Assert.Equal(1, result.Written);
            Assert.NotNull(result.FinalState);
            Assert.Equal(15d, Convert.ToDouble(result.FinalState!["Total"]));

            var lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Total", lines[0]);
            Assert.Contains("15", lines[1]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
