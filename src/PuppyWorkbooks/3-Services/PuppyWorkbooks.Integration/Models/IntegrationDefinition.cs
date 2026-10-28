using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlRoot("Integration")]
public sealed class IntegrationDefinition
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlElement("TelemetryOptions")] public TelemetryOptions? TelemetryOptions { get; set; }
    [XmlArray("HttpConfigurations")]
    [XmlArrayItem("HttpConfiguration")]
    public List<HttpProviderSettings> HttpConfigurations { get; set; } = [];
    [XmlArray("Steps")]
    [XmlArrayItem("Map", typeof(MapStep))]
    [XmlArrayItem("Filter", typeof(FilterStep))]
    [XmlArrayItem("Reduce", typeof(ReduceStep))]
    [XmlArrayItem("Switch", typeof(SwitchStep))]
    [XmlArrayItem("RunIntegration", typeof(RunIntegrationStep))]
    [XmlArrayItem("CsvInput", typeof(CsvInputStep))]
    [XmlArrayItem("SqlInput", typeof(SqlInputStep))]
    [XmlArrayItem("HttpInput", typeof(HttpInputStep))]
    [XmlArrayItem("JsonInput", typeof(JsonInputStep))]
    [XmlArrayItem("XmlInput", typeof(XmlInputStep))]
    [XmlArrayItem("FileSystemInput", typeof(FileSystemInputStep))]
    [XmlArrayItem("MemoryInput", typeof(MemoryInputStep))]
    [XmlArrayItem("IOInput", typeof(InputStep))]
    [XmlArrayItem("CsvOutput", typeof(CsvOutputStep))]
    [XmlArrayItem("SqlOutput", typeof(SqlOutputStep))]
    [XmlArrayItem("HttpOutput", typeof(HttpOutputStep))]
    [XmlArrayItem("JsonOutput", typeof(JsonOutputStep))]
    [XmlArrayItem("XmlOutput", typeof(XmlOutputStep))]
    [XmlArrayItem("MemoryOutput", typeof(MemoryOutputStep))]
    [XmlArrayItem("IOOutput", typeof(OutputStep))]
    public List<IntegrationStep> Steps { get; set; } = [];

    [XmlIgnore] public string? SourceDirectory { get; set; }

    [XmlElement("SecretManager")]
    public SecretManagerConfiguration? SecretManager { get; set; }
}

public sealed class SecretManagerConfiguration
{
    [XmlElement("WindowsCredentialsSecret", typeof(WindowsCredentialsSecret))]
    [XmlElement("KeyVaultCredential", typeof(KeyVaultCredential))]
    [XmlElement("EnvSecret", typeof(EnvSecret))]
    public List<SecretDefinition> Secrets { get; set; } = [];
}

public abstract class SecretDefinition
{
    [XmlAttribute] public string SecretName { get; set; } = string.Empty;
}

public sealed class WindowsCredentialsSecret : SecretDefinition
{
    [XmlAttribute] public string CredentialTargetName { get; set; } = string.Empty;
    [XmlAttribute] public string UsernameToSecretName { get; set; } = string.Empty;
    [XmlAttribute] public string PasswordToSecretName { get; set; } = string.Empty;
}

public sealed class KeyVaultCredential : SecretDefinition
{
    [XmlAttribute] public string KeyVaultURI { get; set; } = string.Empty;
}

public sealed class EnvSecret : SecretDefinition
{
    [XmlAttribute] public string EnvVarName { get; set; } = string.Empty;
}
