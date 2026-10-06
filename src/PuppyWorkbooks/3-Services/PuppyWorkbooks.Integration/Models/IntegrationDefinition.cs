using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlRoot("Integration")]
public sealed class IntegrationDefinition
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlArray("HttpConfigurations")]
    [XmlArrayItem("HttpConfiguration")]
    public List<HttpProviderSettings> HttpConfigurations { get; set; } = [];
    [XmlArray("Steps")]
    [XmlArrayItem("Map", typeof(MapStep))]
    [XmlArrayItem("Filter", typeof(FilterStep))]
    [XmlArrayItem("Reduce", typeof(ReduceStep))]
    [XmlArrayItem("Switch", typeof(SwitchStep))]
    [XmlArrayItem("IOInput", typeof(InputStep))]
    [XmlArrayItem("IOOutput", typeof(OutputStep))]
    public List<IntegrationStep> Steps { get; set; } = [];

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
    [XmlAttribute] public string SecretURI { get; set; } = string.Empty;
}

public sealed class EnvSecret : SecretDefinition
{
    [XmlAttribute] public string EnvVarName { get; set; } = string.Empty;
}
