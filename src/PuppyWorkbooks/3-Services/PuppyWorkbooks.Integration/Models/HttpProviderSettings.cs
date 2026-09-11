using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

/// <summary>Settings shared by HTTP input and output providers.</summary>
public sealed class HttpProviderSettings
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlAttribute] public string BaseUrl { get; set; } = string.Empty;
    [XmlAttribute] public string HttpClientName { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientId { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientSecret { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthScope { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthTokenUrl { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthHttpClientName { get; set; } = string.Empty;
    [XmlAttribute] public string ClientCertificateThumbprint { get; set; } = string.Empty;
    [XmlArray("Headers")]
    [XmlArrayItem("Header")]
    public List<HttpHeader> Headers { get; set; } = [];
}

public sealed class HttpHeader
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlAttribute] public string Value { get; set; } = string.Empty;
}
