using System.Xml;
using System.Xml.Schema;

namespace PuppyWorkbooks.Tests;

public sealed class SchemaValidationTests
{
    [Theory]
    [InlineData("Map.xml")]
    [InlineData("Filter.xml")]
    [InlineData("Reduce.xml")]
    public void WorkSheetSchema_ValidatesIntegrationSampleWorksheets(string fileName)
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "WorkSheet.xsd");
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", fileName);

        ValidateXml(xmlPath, schemaPath);
    }

    [Theory]
    [InlineData("TestWorkbook.xml")]
    [InlineData("InterpreterSample.xml")]
    public void WorkSheetSchema_ValidatesRootSampleWorksheets(string fileName)
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "WorkSheet.xsd");
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", fileName);

        ValidateXml(xmlPath, schemaPath);
    }

    [Theory]
    [InlineData("CliMockData.xml")]
    [InlineData("CliMockDataAll.xml")]
    [InlineData("CliScenario.xml")]
    [InlineData("HttpMockData.xml")]
    [InlineData("HttpMockDataFile.xml")]
    [InlineData("HttpProviders.xml")]
    [InlineData("MockCsvFilePath.xml")]
    [InlineData("MockCsvInline.xml")]
    [InlineData("MockDataMissing.xml")]
    [InlineData("MockDataVariousFormats.xml")]
    [InlineData("MockDeferredOutput.xml")]
    [InlineData("MockNotMatched.xml")]
    [InlineData("MockOutput.xml")]
    [InlineData("MockSqlOutput.xml")]
    [InlineData("MultipleMockDataSources.xml")]
    [InlineData("MultipleMockScenarios.xml")]
    [InlineData("ScenarioFallback.xml")]
    [InlineData("Switch.xml")]
    [InlineData("TestIntegration.xml")]
    [InlineData("TestIntegrationWithFileReferences.xml")]
    public void IntegrationSchema_ValidatesAllIntegrationSamples(string fileName)
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "Integration.xsd");
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", fileName);

        ValidateXml(xmlPath, schemaPath);
    }

    private static void ValidateXml(string xmlPath, string schemaPath)
    {
        var schemas = new XmlSchemaSet();
        schemas.Add(null, schemaPath);

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
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

        using var reader = XmlReader.Create(xmlPath, settings);
        while (reader.Read()) { }

        Assert.Empty(errors);
    }
}
