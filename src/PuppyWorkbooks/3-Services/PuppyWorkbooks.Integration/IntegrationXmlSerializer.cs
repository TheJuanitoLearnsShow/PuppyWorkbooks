using System.Xml.Serialization;
using System.Xml.Linq;
using PuppyWorkbooks.Serialization;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration;

public sealed class IntegrationXmlSerializer
{
    private readonly XmlSerializer _serializer = new(typeof(IntegrationDefinition));

    public IntegrationDefinition Deserialize(string xml)
        => Deserialize(xml, baseDirectory: null);

    private IntegrationDefinition Deserialize(string xml, string? baseDirectory)
    {
        using var reader = new StringReader(xml);
        var definition = (IntegrationDefinition)_serializer.Deserialize(reader)!;
        LoadReferencedWorksheets(definition, xml, baseDirectory);
        return definition;
    }

    public IntegrationDefinition DeserializeFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Deserialize(File.ReadAllText(fullPath), Path.GetDirectoryName(fullPath));
    }

    public string Serialize(IntegrationDefinition definition)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        _serializer.Serialize(writer, definition);
        return writer.ToString();
    }

    private static void LoadReferencedWorksheets(IntegrationDefinition definition, string xml,
        string? baseDirectory)
    {
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var steps = definition.Steps;
        var stepElements = document.Root?.Element("Steps")?.Elements().ToList() ?? [];

        for (var index = 0; index < steps.Count; index++)
            LoadWorksheetReferences(steps[index], stepElements.ElementAtOrDefault(index), baseDirectory);
    }

    private static void LoadWorksheetReferences(IntegrationStep step, XElement? stepElement, string? baseDirectory)
    {
        var worksheetElement = stepElement?.Element("Worksheet");
        if (worksheetElement is not null)
        {

            var path = (string?)worksheetElement.Attribute("FilePath")
                       ?? (string?)worksheetElement.Attribute("Path")
                       ?? (string?)worksheetElement.Attribute("File")
                       ?? (string?)worksheetElement.Attribute("Filename")
                       ?? (string?)worksheetElement.Attribute("FileName");

            path ??= (string?)worksheetElement.Element("FilePath")
                     ?? (string?)worksheetElement.Element("Path")
                     ?? (string?)worksheetElement.Element("Filename")
                     ?? (string?)worksheetElement.Element("FileName");

            // Also accept <Worksheet>worksheet.xml</Worksheet> as a concise
            // reference form.
            if (string.IsNullOrWhiteSpace(path) && !worksheetElement.Elements().Any())
                path = worksheetElement.Value.Trim();

            if (!string.IsNullOrWhiteSpace(path))
            {

            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), path);
            fullPath = Path.GetFullPath(fullPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Worksheet file '{path}' was not found.", fullPath);

                step.WorksheetPath = fullPath;
                step.Worksheet = new WorkSheetSerializer().DeserializeFromXmlFile(fullPath);
            }
        }

        if (step is SwitchStep switchStep && stepElement is not null)
        {
            var branchElements = stepElement.Elements("Branch").ToList();
            for (var index = 0; index < switchStep.Branches.Count; index++)
            {
                var branch = switchStep.Branches[index];
                var branchElement = branchElements.ElementAtOrDefault(index);
                var childElements = branchElement?.Elements().Where(e => e.Name.LocalName != "Worksheet").ToList() ?? [];
                for (var childIndex = 0; childIndex < branch.Steps.Count; childIndex++)
                    LoadWorksheetReferences(branch.Steps[childIndex], childElements.ElementAtOrDefault(childIndex), baseDirectory);
            }
        }
    }
}
