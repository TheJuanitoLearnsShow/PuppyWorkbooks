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

    public IntegrationDefinition Deserialize(string xml, string? baseDirectory)
    {
        using var reader = new StringReader(xml);
        var definition = (IntegrationDefinition)_serializer.Deserialize(reader)!;
        ResolveHttpConfigurations(definition);
        LoadReferencedWorksheets(definition, xml, baseDirectory);
        return definition;
    }

    private static void ResolveHttpConfigurations(IntegrationDefinition definition)
    {
        var configurations = definition.HttpConfigurations
            .Where(configuration => !string.IsNullOrWhiteSpace(configuration.Name))
            .ToDictionary(configuration => configuration.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var step in definition.Steps)
        {
            var configurationName = step switch
            {
                InputStep source => source.HttpConfiguration,
                OutputStep sink => sink.HttpConfiguration,
                _ => null
            };
            if (string.IsNullOrWhiteSpace(configurationName)) continue;
            if (!configurations.TryGetValue(configurationName, out var configuration))
                throw new InvalidOperationException($"HTTP configuration '{configurationName}' was not found.");
            if (step is InputStep inputStep) inputStep.ResolvedHttpConfiguration = configuration;
            if (step is OutputStep outputStep) outputStep.ResolvedHttpConfiguration = configuration;
        }
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
        {
            LoadWorksheetReferences(steps[index], stepElements.ElementAtOrDefault(index), baseDirectory);
            LoadMockReferences(steps[index], stepElements.ElementAtOrDefault(index), baseDirectory);
        }
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

    private static void LoadMockReferences(IntegrationStep step, XElement? stepElement, string? baseDirectory)
    {
        if (step is not InputStep inputStep) return;

        inputStep.MockDataSources.Clear();

        var candidateElements = new List<XElement>();

        if (stepElement is not null)
        {
            var containers = stepElement.Elements().Where(e =>
                e.Name.LocalName.Equals("MockDataSources", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("Mocks", StringComparison.OrdinalIgnoreCase));

            foreach (var container in containers)
            {
                candidateElements.AddRange(container.Elements());
            }

            var directMocks = stepElement.Elements().Where(e =>
                e.Name.LocalName.Equals("MockData", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("MockCsv", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("Mock", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("MockDataSource", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("MockDataEntry", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("MockItem", StringComparison.OrdinalIgnoreCase));

            candidateElements.AddRange(directMocks);
        }

        foreach (var mockElem in candidateElements)
        {
            var name = (string?)mockElem.Attribute("Name")
                       ?? (string?)mockElem.Attribute("Key")
                       ?? (string?)mockElem.Attribute("Scenario")
                       ?? (string?)mockElem.Attribute("ScenarioName")
                       ?? (string?)mockElem.Attribute("Id")
                       ?? (string?)mockElem.Element("Name")
                       ?? (string?)mockElem.Element("Key")
                       ?? (string?)mockElem.Element("Scenario")
                       ?? (string?)mockElem.Element("ScenarioName")
                       ?? string.Empty;

            var path = (string?)mockElem.Attribute("FilePath")
                       ?? (string?)mockElem.Attribute("Path")
                       ?? (string?)mockElem.Attribute("File")
                       ?? (string?)mockElem.Attribute("Filename")
                       ?? (string?)mockElem.Attribute("FileName")
                       ?? (string?)mockElem.Attribute("MockCsvFilePath")
                       ?? (string?)mockElem.Attribute("MockFilePath")
                       ?? (string?)mockElem.Attribute("MockDataFilePath")
                       ?? (string?)mockElem.Attribute("MockPath")
                       ?? (string?)mockElem.Attribute("MockCsv")
                       ?? (string?)mockElem.Element("FilePath")
                       ?? (string?)mockElem.Element("Path")
                       ?? (string?)mockElem.Element("Filename")
                       ?? (string?)mockElem.Element("FileName");

            if (string.IsNullOrWhiteSpace(path) && !mockElem.Elements().Any())
            {
                var textCandidate = mockElem.Value.Trim();
                if (!textCandidate.Contains('\n') && !textCandidate.Contains('\r') &&
                    (textCandidate.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                     textCandidate.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                     (!textCandidate.Contains(',') && !textCandidate.Contains('{') && !textCandidate.Contains('['))))
                {
                    var fileCheck = Path.IsPathRooted(textCandidate)
                        ? textCandidate
                        : Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), textCandidate);
                    if (File.Exists(fileCheck))
                    {
                        path = textCandidate;
                    }
                }
            }

            var resolvedFilePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var fullPath = Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), path);
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Mock CSV file '{path}' was not found.", fullPath);

                resolvedFilePath = fullPath;
            }

            var rawContent = string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedFilePath))
            {
                rawContent = (string?)mockElem.Attribute("Content")
                             ?? (string?)mockElem.Attribute("RawText")
                             ?? (string?)mockElem.Attribute("Text")
                             ?? (string?)mockElem.Attribute("Value")
                             ?? (string?)mockElem.Element("Content")
                             ?? (string?)mockElem.Element("RawText")
                             ?? (string?)mockElem.Element("Text")
                             ?? (string?)mockElem.Element("Value")
                             ?? (!mockElem.Elements().Any() ? mockElem.Value : string.Empty);
            }

            var mockSource = new MockDataSource
            {
                Name = name,
                FilePath = resolvedFilePath,
                Content = rawContent
            };

            var dictKey = !string.IsNullOrWhiteSpace(name) ? name : $"Mock_{inputStep.MockDataSources.Count + 1}";
            inputStep.MockDataSources[dictKey] = mockSource;
        }

        // Handle step-level legacy attributes/elements if no mock sources were loaded from child elements
        if (inputStep.MockDataSources.Count == 0)
        {
            var stepPath = (string?)stepElement?.Attribute("MockCsvFilePath")
                           ?? (string?)stepElement?.Attribute("MockFilePath")
                           ?? (string?)stepElement?.Attribute("MockDataFilePath")
                           ?? (string?)stepElement?.Attribute("MockDataPath")
                           ?? (string?)stepElement?.Attribute("MockPath")
                           ?? (string?)stepElement?.Attribute("MockCsv")
                           ?? (string?)stepElement?.Element("MockCsvFilePath")
                           ?? (string?)stepElement?.Element("MockFilePath")
                           ?? (string?)stepElement?.Element("MockDataFilePath")
                           ?? (!string.IsNullOrWhiteSpace(inputStep.MockCsvFilePath) ? inputStep.MockCsvFilePath : null);

            if (!string.IsNullOrWhiteSpace(stepPath))
            {
                var fullPath = Path.IsPathRooted(stepPath)
                    ? stepPath
                    : Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), stepPath);
                fullPath = Path.GetFullPath(fullPath);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Mock CSV file '{stepPath}' was not found.", fullPath);

                inputStep.MockCsvFilePath = fullPath;
                inputStep.MockDataSources["Default"] = new MockDataSource
                {
                    Name = "Default",
                    FilePath = fullPath
                };
            }
            else if (!string.IsNullOrWhiteSpace(inputStep.MockCsv) || !string.IsNullOrWhiteSpace(inputStep.MockData))
            {
                var inline = !string.IsNullOrWhiteSpace(inputStep.MockCsv) ? inputStep.MockCsv : inputStep.MockData;
                inputStep.MockDataSources["Default"] = new MockDataSource
                {
                    Name = "Default",
                    Content = inline
                };
            }
        }

        // Keep legacy properties in sync with first mock source if available
        if (inputStep.MockDataSources.Count > 0)
        {
            var first = inputStep.MockDataSources.Values.First();
            if (!string.IsNullOrWhiteSpace(first.FilePath))
            {
                inputStep.MockCsvFilePath = first.FilePath;
            }
            else if (!string.IsNullOrWhiteSpace(first.Content))
            {
                inputStep.MockCsv = first.Content;
                inputStep.MockData = first.Content;
            }
        }
    }
}
