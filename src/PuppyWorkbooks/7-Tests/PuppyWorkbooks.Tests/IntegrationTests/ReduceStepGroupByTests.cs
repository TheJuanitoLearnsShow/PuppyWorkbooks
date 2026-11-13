using System.Xml.Linq;
using System.Xml.Schema;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Models.Input;
using PuppyWorkbooks.Integration.Models.Output;

namespace PuppyWorkbooks.Tests.IntegrationTests;

public sealed class ReduceStepGroupByTests
{
    [Fact]
    public void ReduceStep_DeserializesGroupByFieldAndAliases()
    {
        var serializer = new IntegrationXmlSerializer();

        var xmlAttr = """
            <Integration Name="Test">
              <Steps>
                <Reduce Id="r1" GroupByField="Category" OutputField="Total">
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
              </Steps>
            </Integration>
            """;
        var def1 = serializer.Deserialize(xmlAttr);
        var r1 = Assert.IsType<ReduceStep>(Assert.Single(def1.Steps));
        Assert.Equal("Category", r1.GroupByField);
        Assert.Equal("Total", r1.OutputField);

        var xmlAliasAttr = """
            <Integration Name="Test">
              <Steps>
                <Reduce Id="r2" GroupBy="Department" OutputField="Sum">
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
              </Steps>
            </Integration>
            """;
        var def2 = serializer.Deserialize(xmlAliasAttr);
        var r2 = Assert.IsType<ReduceStep>(Assert.Single(def2.Steps));
        Assert.Equal("Department", r2.GroupByField);

        var xmlElem = """
            <Integration Name="Test">
              <Steps>
                <Reduce Id="r3" OutputField="Total">
                  <GroupByField>Region</GroupByField>
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
              </Steps>
            </Integration>
            """;
        var def3 = serializer.Deserialize(xmlElem);
        var r3 = Assert.IsType<ReduceStep>(Assert.Single(def3.Steps));
        Assert.Equal("Region", r3.GroupByField);

        var xmlAliasElem = """
            <Integration Name="Test">
              <Steps>
                <Reduce Id="r4">
                  <GroupBy>Type</GroupBy>
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
              </Steps>
            </Integration>
            """;
        var def4 = serializer.Deserialize(xmlAliasElem);
        var r4 = Assert.IsType<ReduceStep>(Assert.Single(def4.Steps));
        Assert.Equal("Type", r4.GroupByField);
    }

    [Fact]
    public async Task ReduceStep_WithGroupByField_ReducesPerDistinctValue()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "sales.csv");
        var outputPath = Path.Combine(directory, "summary.csv");

        var csvContent = """
            Category,Amount
            Electronics,100
            Books,20
            Electronics,50
            Books,30
            Clothing,40
            """;
        await File.WriteAllTextAsync(inputPath, csvContent);

        try
        {
            var xml = $"""
                <Integration Name="GroupByIntegration">
                  <Steps>
                    <CsvInput Id="input" FilePath="{inputPath.Replace('\\', '/')}" />
                    <Reduce Id="reduceByCategory" GroupByField="Category" OutputField="Total">
                      <InitialStateJson>0</InitialStateJson>
                      <Worksheet>
                        <Cells>
                          <WorkCell><Id>1</Id><Name>Total</Name><Formula>State + Value(InputRecord.Amount)</Formula></WorkCell>
                        </Cells>
                      </Worksheet>
                    </Reduce>
                    <CsvOutput Id="output" FilePath="{outputPath.Replace('\\', '/')}" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var writtenRecords = new List<IntegrationRecord>();
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                OutputAction = rec => writtenRecords.Add(rec)
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(5, result.Read);
            Assert.Equal(3, result.Written);

            Assert.True(File.Exists(outputPath));
            var outputLines = (await File.ReadAllLinesAsync(outputPath)).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            Assert.Equal(4, outputLines.Count); // Header + 3 rows

            Assert.Contains("Total,Category", outputLines[0]);
            Assert.Contains("150,Electronics", outputLines[1]);
            Assert.Contains("50,Books", outputLines[2]);
            Assert.Contains("40,Clothing", outputLines[3]);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReduceStep_WithGroupByField_SupportsDownstreamFilterAndMaps()
    {
        var directory = "./PuppyWorkbooks-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        var inputPath = Path.Combine(directory, "sales.csv");

        var csvContent = """
            Category,Amount
            Electronics,100
            Books,20
            Electronics,50
            Books,30
            Clothing,40
            """;
        await File.WriteAllTextAsync(inputPath, csvContent);

        try
        {
            var xml = $"""
                <Integration Name="GroupByFilterTest">
                  <Steps>
                    <CsvInput Id="input" FilePath="{inputPath.Replace('\\', '/')}" />
                    <Reduce Id="reduceByCategory" GroupByField="Category" OutputField="Total">
                      <InitialStateJson>0</InitialStateJson>
                      <Worksheet>
                        <Cells>
                          <WorkCell><Id>1</Id><Name>Total</Name><Formula>State + Value(InputRecord.Amount)</Formula></WorkCell>
                        </Cells>
                      </Worksheet>
                    </Reduce>
                    <Map Id="addTax">
                      <Worksheet>
                        <Cells>
                          <WorkCell><Id>1</Id><Name>Category</Name><Formula>InputRecord.Category</Formula></WorkCell>
                          <WorkCell><Id>2</Id><Name>Total</Name><Formula>InputRecord.Total</Formula></WorkCell>
                          <WorkCell><Id>3</Id><Name>Tax</Name><Formula>InputRecord.Total * 0.1</Formula></WorkCell>
                        </Cells>
                      </Worksheet>
                    </Map>
                    <Filter Id="filterLowTotals">
                      <Worksheet>
                        <Cells>
                          <WorkCell><Id>1</Id><Name>Keep</Name><Formula>InputRecord.Total &gt; 45</Formula></WorkCell>
                        </Cells>
                      </Worksheet>
                    </Filter>
                    <MemoryOutput Id="memOut" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var writtenRecords = new List<IntegrationRecord>();
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                OutputAction = rec => writtenRecords.Add(rec)
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(5, result.Read);
            Assert.Equal(2, result.Written); // Electronics (150 > 45) and Books (50 > 45); Clothing (40 <= 45) filtered out

            Assert.Equal(2, writtenRecords.Count);
            Assert.Equal("Electronics", writtenRecords[0]["Category"]);
            Assert.Equal(150d, Convert.ToDouble(writtenRecords[0]["Total"]));
            Assert.Equal(15d, Convert.ToDouble(writtenRecords[0]["Tax"]));

            Assert.Equal("Books", writtenRecords[1]["Category"]);
            Assert.Equal(50d, Convert.ToDouble(writtenRecords[1]["Total"]));
            Assert.Equal(5d, Convert.ToDouble(writtenRecords[1]["Tax"]));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReduceStep_WithGroupByField_DebugModeCapturesRows()
    {
        var inputData = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase) { ["Category"] = "A", ["Value"] = 10 },
            new(StringComparer.OrdinalIgnoreCase) { ["Category"] = "B", ["Value"] = 20 },
            new(StringComparer.OrdinalIgnoreCase) { ["Category"] = "A", ["Value"] = 30 }
        };

        var worksheet = new WorkSheet
        {
            Name = "SumSheet",
            Cells = [new WorkCell(1, "Total", "State + Value(InputRecord.Value)", "Total")]
        };

        var definition = new IntegrationDefinition
        {
            Steps = [
                new MemoryInputStep { Data = inputData },
                new ReduceStep
                {
                    Id = "reduce",
                    GroupByField = "Category",
                    OutputField = "Total",
                    InitialStateJson = "0",
                    Worksheet = worksheet
                },
                new MemoryOutputStep { Id = "out" }
            ]
        };

        var writtenRecords = new List<IntegrationRecord>();
        var runner = new IntegrationRunner(new IntegrationRunnerOptions
        {
            Debug = true,
            OutputAction = rec => writtenRecords.Add(rec)
        });

        var result = await runner.RunAsync(definition);

        Assert.Equal(3, result.Read);
        Assert.Equal(2, result.Written);
        Assert.NotNull(result.DebugData);
        Assert.Equal(3, result.DebugData.Count);

        // Debug row 0 (Category A, 10) -> Total = 10
        Assert.Equal(10d, Convert.ToDouble(result.DebugData[0].Steps[1].Cells["Total"]));
        // Debug row 1 (Category B, 20) -> Total = 20
        Assert.Equal(20d, Convert.ToDouble(result.DebugData[1].Steps[1].Cells["Total"]));
        // Debug row 2 (Category A, 30) -> Total = 40
        Assert.Equal(40d, Convert.ToDouble(result.DebugData[2].Steps[1].Cells["Total"]));
    }

    [Fact]
    public void GroupBySchema_ValidatesAgainstIntegrationXsd()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "Integration.xsd");
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="GroupBySchemaValidation">
              <Steps>
                <MemoryInput Id="memIn" />
                <Reduce Id="r1" GroupByField="Category" OutputField="Total">
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
                <Reduce Id="r2" GroupBy="Dept" OutputField="Sum">
                  <InitialStateJson>0</InitialStateJson>
                </Reduce>
                <MemoryOutput Id="memOut" />
              </Steps>
            </Integration>
            """;

        var schemas = new XmlSchemaSet();
        schemas.Add(null, schemaPath);

        var settings = new System.Xml.XmlReaderSettings
        {
            ValidationType = System.Xml.ValidationType.Schema,
            Schemas = schemas,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                              XmlSchemaValidationFlags.ProcessSchemaLocation |
                              XmlSchemaValidationFlags.ReportValidationWarnings
        };

        var errors = new List<string>();
        settings.ValidationEventHandler += (_, args) =>
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                errors.Add($"Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}: {args.Message}");
            }
        };

        using var stringReader = new StringReader(xml);
        using var reader = System.Xml.XmlReader.Create(stringReader, settings);
        while (reader.Read()) { }

        Assert.Empty(errors);
    }
}
