using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

/// <summary>Settings shared by HTTP input and output providers.</summary>
public sealed class HttpProviderSettings
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlAttribute] public string BaseUrl { get; set; } = string.Empty;
    [XmlAttribute] public string BaseUrlFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpClientName { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientId { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientIdFromField { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientSecret { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthClientSecretFromField { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthScope { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthScopeFromField { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthTokenUrl { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthTokenUrlFromField { get; set; } = string.Empty;
    [XmlAttribute] public string OAuthHttpClientName { get; set; } = string.Empty;
    [XmlAttribute] public string ClientCertificateThumbprint { get; set; } = string.Empty;
    [XmlAttribute] public string ClientCertificateThumbprintFromField { get; set; } = string.Empty;
    [XmlArray("Headers")]
    [XmlArrayItem("Header")]
    public List<HttpHeader> Headers { get; set; } = [];

    public bool ShouldSerializeBaseUrl() => !string.IsNullOrWhiteSpace(BaseUrl);
    public bool ShouldSerializeBaseUrlFromField() => !string.IsNullOrWhiteSpace(BaseUrlFromField);
    public bool ShouldSerializeHttpClientName() => !string.IsNullOrWhiteSpace(HttpClientName);
    public bool ShouldSerializeOAuthClientId() => !string.IsNullOrWhiteSpace(OAuthClientId);
    public bool ShouldSerializeOAuthClientIdFromField() => !string.IsNullOrWhiteSpace(OAuthClientIdFromField);
    public bool ShouldSerializeOAuthClientSecret() => !string.IsNullOrWhiteSpace(OAuthClientSecret);
    public bool ShouldSerializeOAuthClientSecretFromField() => !string.IsNullOrWhiteSpace(OAuthClientSecretFromField);
    public bool ShouldSerializeOAuthScope() => !string.IsNullOrWhiteSpace(OAuthScope);
    public bool ShouldSerializeOAuthScopeFromField() => !string.IsNullOrWhiteSpace(OAuthScopeFromField);
    public bool ShouldSerializeOAuthTokenUrl() => !string.IsNullOrWhiteSpace(OAuthTokenUrl);
    public bool ShouldSerializeOAuthTokenUrlFromField() => !string.IsNullOrWhiteSpace(OAuthTokenUrlFromField);
    public bool ShouldSerializeOAuthHttpClientName() => !string.IsNullOrWhiteSpace(OAuthHttpClientName);
    public bool ShouldSerializeClientCertificateThumbprint() => !string.IsNullOrWhiteSpace(ClientCertificateThumbprint);
    public bool ShouldSerializeClientCertificateThumbprintFromField() => !string.IsNullOrWhiteSpace(ClientCertificateThumbprintFromField);
    public bool ShouldSerializeHeaders() => Headers.Count > 0;
}

public sealed class HttpHeader
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlAttribute] public string Value { get; set; } = string.Empty;
    [XmlAttribute] public string ValueFromField { get; set; } = string.Empty;

    public bool ShouldSerializeValue() => !string.IsNullOrWhiteSpace(Value);
    public bool ShouldSerializeValueFromField() => !string.IsNullOrWhiteSpace(ValueFromField);
}
