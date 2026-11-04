using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PuppyWorkbooks.CLI;

internal sealed class IntegrationHtmlReportGenerator
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly Dictionary<string, IntegrationDocument> _documents = new(PathComparer);

    public static async Task GenerateAsync(
        string integrationPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var generator = new IntegrationHtmlReportGenerator();
        await generator.LoadIntegrationAsync(integrationPath, cancellationToken);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await File.WriteAllTextAsync(
            fullOutputPath,
            generator.BuildHtml(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
    }

    private async Task<IntegrationDocument> LoadIntegrationAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        if (_documents.TryGetValue(fullPath, out var existing))
        {
            return existing;
        }

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        var xml = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var root = xml.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "Integration", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"'{fullPath}' does not have an Integration XML root.");
        }

        var document = new IntegrationDocument(
            fullPath,
            xml,
            $"integration-{_documents.Count + 1}",
            Convert.ToHexString(SHA256.HashData(bytes)));
        _documents.Add(fullPath, document);

        foreach (var runStep in root.Descendants().Where(element =>
                     string.Equals(element.Name.LocalName, "RunIntegration", StringComparison.Ordinal)))
        {
            var reference = (string?)runStep.Attribute("FilePath") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reference))
            {
                document.ChildIntegrations.Add(
                    new ChildIntegration(runStep, reference, null, "FilePath is not specified."));
                continue;
            }

            if (reference.Contains("{{", StringComparison.Ordinal) ||
                reference.Contains("}}", StringComparison.Ordinal))
            {
                document.ChildIntegrations.Add(
                    new ChildIntegration(runStep, reference, null,
                        "FilePath contains runtime interpolation and cannot be resolved without a row."));
                continue;
            }

            var childPath = Path.IsPathRooted(reference)
                ? reference
                : Path.Combine(Path.GetDirectoryName(fullPath)!, reference);
            childPath = Path.GetFullPath(childPath);
            if (!File.Exists(childPath))
            {
                document.ChildIntegrations.Add(
                    new ChildIntegration(runStep, reference, null, "The referenced integration file was not found."));
                continue;
            }

            var child = await LoadIntegrationAsync(childPath, cancellationToken);
            document.ChildIntegrations.Add(new ChildIntegration(runStep, reference, child, null));
        }

        return document;
    }

    private string BuildHtml()
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Integration audit report</title>");
        html.AppendLine("""
                        <style>
                        :root { color-scheme: light; font-family: "Segoe UI", Arial, sans-serif; color: #202124; background: #f7f8fa; }
                        body { max-width: 1100px; margin: 0 auto; padding: 2rem; line-height: 1.5; }
                        h1, h2, h3 { line-height: 1.2; }
                        .integration, .step, .metadata { background: white; border: 1px solid #d5d9de; border-radius: 6px; padding: 1rem 1.25rem; margin: 1rem 0; }
                        .metadata { border-left: 4px solid #5578a8; overflow-wrap: anywhere; }
                        .step { margin-left: 1rem; }
                        .step summary { cursor: pointer; font-weight: 600; }
                        pre { white-space: pre-wrap; overflow-wrap: anywhere; background: #f3f5f7; border: 1px solid #e1e4e8; padding: 1rem; }
                        code, .hash { overflow-wrap: anywhere; }
                        .notice { color: #8a3b12; }
                        nav li { margin: .35rem 0; }
                        @media print { body { max-width: none; padding: 0; } .step { break-inside: avoid; } }
                        </style></head><body>
                        """);
        html.AppendLine("<h1>Integration audit report</h1>");
        html.Append("<p>Generated (UTC): ").Append(Escape(DateTimeOffset.UtcNow.ToString("O"))).AppendLine("</p>");
        html.AppendLine("<h2>Integration index</h2><nav><ol>");
        foreach (var document in _documents.Values)
        {
            html.Append("<li><a href=\"#").Append(Escape(document.Anchor)).Append("\">")
                .Append(Escape(GetIntegrationName(document))).Append("</a> - ")
                .Append(Escape(document.Path)).AppendLine("</li>");
        }

        html.AppendLine("</ol></nav>");

        foreach (var document in _documents.Values)
        {
            RenderIntegration(html, document);
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private void RenderIntegration(StringBuilder html, IntegrationDocument document)
    {
        html.Append("<section class=\"integration\" id=\"").Append(Escape(document.Anchor)).AppendLine("\">");
        html.Append("<h2>").Append(Escape(GetIntegrationName(document))).AppendLine("</h2>");
        html.AppendLine("<div class=\"metadata\"><h3>Source and integrity</h3><dl>");
        html.Append("<dt>File</dt><dd>").Append(Escape(document.Path)).AppendLine("</dd>");
        html.Append("<dt>SHA-256</dt><dd class=\"hash\"><code>").Append(document.Sha256).AppendLine("</code></dd>");
        html.Append("<dt>Steps</dt><dd>")
            .Append(document.Xml.Root?.Element("Steps")?.Elements().Count() ?? 0)
            .AppendLine(" top-level step(s)</dd></dl></div>");

        var configuration = new XDocument(new XElement(document.Xml.Root!));
        configuration.Root?.Element("Steps")?.Remove();
        if (configuration.Root?.HasAttributes == true || configuration.Root?.HasElements == true)
        {
            html.AppendLine("<details><summary>Integration-level configuration</summary><pre>")
                .Append(Escape(Sanitize(configuration.Root!).ToString(SaveOptions.None)))
                .AppendLine("</pre></details>");
        }

        html.AppendLine("<h3>Ordered steps and configuration</h3>");
        var steps = document.Xml.Root?.Element("Steps")?.Elements().ToList() ?? [];
        if (steps.Count == 0)
        {
            html.AppendLine("<p>No steps are defined.</p>");
        }

        for (var index = 0; index < steps.Count; index++)
        {
            RenderStep(html, document, steps[index], (index + 1).ToString());
        }

        html.AppendLine("</section>");
    }

    private void RenderStep(StringBuilder html, IntegrationDocument document, XElement step, string number)
    {
        var stepType = step.Name.LocalName;
        var stepId = (string?)step.Attribute("Id");
        html.Append("<details class=\"step\" open><summary>Step ").Append(Escape(number)).Append(": ")
            .Append(Escape(stepType));
        if (!string.IsNullOrWhiteSpace(stepId))
        {
            html.Append(" — ").Append(Escape(stepId));
        }

        html.AppendLine("</summary>");
        html.Append("<p><strong>Type:</strong> ").Append(Escape(stepType));
        if (!string.IsNullOrWhiteSpace(stepId))
        {
            html.Append("; <strong>ID:</strong> ").Append(Escape(stepId));
        }

        html.AppendLine("</p>");
        RenderChildIntegrationReference(html, document, step);
        html.AppendLine("<h4>Step XML</h4><pre>")
            .Append(Escape(Sanitize(new XElement(step)).ToString(SaveOptions.None)))
            .AppendLine("</pre>");
        RenderReferencedWorksheets(html, document, step);

        var nestedSteps = step.Elements("Branch")
            .SelectMany(branch => branch.Elements()
                .Where(element => !string.Equals(element.Name.LocalName, "Worksheet", StringComparison.Ordinal)))
            .ToList();
        if (nestedSteps.Count > 0)
        {
            html.AppendLine("<h4>Branch steps</h4>");
            for (var index = 0; index < nestedSteps.Count; index++)
            {
                RenderStep(html, document, nestedSteps[index], $"{number}.{index + 1}");
            }
        }

        html.AppendLine("</details>");
    }

    private static void RenderReferencedWorksheets(StringBuilder html, IntegrationDocument document, XElement step)
    {
        foreach (var worksheet in step.Elements().Where(element =>
                     string.Equals(element.Name.LocalName, "Worksheet", StringComparison.Ordinal)))
        {
            var referencePath = (string?)worksheet.Attribute("FilePath")
                                ?? (string?)worksheet.Attribute("Path")
                                ?? (string?)worksheet.Attribute("File")
                                ?? (string?)worksheet.Attribute("Filename")
                                ?? (string?)worksheet.Attribute("FileName")
                                ?? (string?)worksheet.Element("FilePath")
                                ?? (string?)worksheet.Element("Path")
                                ?? (string?)worksheet.Element("Filename")
                                ?? (string?)worksheet.Element("FileName");
            if (string.IsNullOrWhiteSpace(referencePath) && !worksheet.Elements().Any())
            {
                referencePath = worksheet.Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(referencePath))
            {
                continue;
            }

            var worksheetPath = Path.IsPathRooted(referencePath)
                ? referencePath
                : Path.Combine(Path.GetDirectoryName(document.Path)!, referencePath);
            worksheetPath = Path.GetFullPath(worksheetPath);
            html.AppendLine("<h4>Referenced worksheet</h4><p>File: ")
                .Append(Escape(worksheetPath)).AppendLine("</p>");
            if (!File.Exists(worksheetPath))
            {
                html.Append("<p class=\"notice\">The referenced worksheet file was not found: ")
                    .Append(Escape(referencePath)).AppendLine("</p>");
                continue;
            }

            var bytes = File.ReadAllBytes(worksheetPath);
            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            var worksheetXml = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            html.Append("<p>SHA-256: <code>").Append(Convert.ToHexString(SHA256.HashData(bytes)))
                .AppendLine("</code></p><pre>")
                .Append(Escape(Sanitize(new XElement(worksheetXml.Root!)).ToString(SaveOptions.None)))
                .AppendLine("</pre>");
        }
    }

    private static void RenderChildIntegrationReference(StringBuilder html, IntegrationDocument document, XElement step)
    {
        if (!string.Equals(step.Name.LocalName, "RunIntegration", StringComparison.Ordinal))
        {
            return;
        }

        var child = document.ChildIntegrations.First(reference => ReferenceEquals(reference.Step, step));
        html.Append("<p><strong>Runs integration:</strong> ");
        if (child.Document is not null)
        {
            html.Append("<a href=\"#").Append(Escape(child.Document.Anchor)).Append("\">")
                .Append(Escape(GetIntegrationName(child.Document))).Append("</a> (")
                .Append(Escape(child.ReferencePath)).AppendLine(")</p>");
        }
        else
        {
            html.Append("<span class=\"notice\">").Append(Escape(child.ReferencePath))
                .Append(" - ").Append(Escape(child.UnresolvedReason ?? "Reference could not be resolved."))
                .AppendLine("</span></p>");
        }
    }

    private static XElement Sanitize(XElement element)
    {
        foreach (var attribute in element.Attributes().ToList())
        {
            if (IsSensitiveName(attribute.Name.LocalName))
            {
                attribute.Value = "[REDACTED]";
            }
        }

        if (IsSensitiveName(element.Name.LocalName))
        {
            element.RemoveNodes();
            element.Add("[REDACTED]");
        }
        else
        {
            foreach (var child in element.Elements().ToList())
            {
                Sanitize(child);
            }
        }

        return element;
    }

    private static bool IsSensitiveName(string name)
    {
        var normalized = new string(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return normalized.Contains("password", StringComparison.Ordinal) ||
               normalized.Contains("token", StringComparison.Ordinal) ||
               normalized.Contains("apikey", StringComparison.Ordinal) ||
               normalized.Contains("connectionstring", StringComparison.Ordinal) ||
               normalized.Contains("authorization", StringComparison.Ordinal) ||
               normalized.Contains("clientsecret", StringComparison.Ordinal) ||
               normalized.Contains("privatekey", StringComparison.Ordinal) ||
               normalized.Contains("accesskey", StringComparison.Ordinal) ||
               normalized == "secret" ||
               normalized.Contains("secret", StringComparison.Ordinal) &&
               !normalized.EndsWith("name", StringComparison.Ordinal) &&
               !normalized.EndsWith("reference", StringComparison.Ordinal) &&
               !normalized.EndsWith("id", StringComparison.Ordinal);
    }

    private static string GetIntegrationName(IntegrationDocument document)
    {
        var name = (string?)document.Xml.Root?.Attribute("Name");
        return string.IsNullOrWhiteSpace(name) ? Path.GetFileName(document.Path) : name;
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);

    private sealed class IntegrationDocument(
        string path,
        XDocument xml,
        string anchor,
        string sha256)
    {
        public string Path { get; } = path;
        public XDocument Xml { get; } = xml;
        public string Anchor { get; } = anchor;
        public string Sha256 { get; } = sha256;
        public List<ChildIntegration> ChildIntegrations { get; } = [];
    }

    private sealed record ChildIntegration(
        XElement Step,
        string ReferencePath,
        IntegrationDocument? Document,
        string? UnresolvedReason);
}