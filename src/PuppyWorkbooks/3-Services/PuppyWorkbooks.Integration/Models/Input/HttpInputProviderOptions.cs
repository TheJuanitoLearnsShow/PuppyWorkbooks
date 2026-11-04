using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class HttpInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.HttpReader;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string EndpointFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "GET";
    [XmlAttribute] public string JsonPath { get; set; } = "$";
    [XmlAttribute] public string JsonPathFromField { get; set; } = string.Empty;
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeHttpConfiguration() => !string.IsNullOrWhiteSpace(HttpConfiguration);
    public bool ShouldSerializeEndpoint() => !string.IsNullOrWhiteSpace(Endpoint);
    public bool ShouldSerializeEndpointFromField() => !string.IsNullOrWhiteSpace(EndpointFromField);
    public bool ShouldSerializeHttpMethod() => !string.IsNullOrWhiteSpace(HttpMethod);
    public bool ShouldSerializeJsonPath() => !string.IsNullOrWhiteSpace(JsonPath);
    public bool ShouldSerializeJsonPathFromField() => !string.IsNullOrWhiteSpace(JsonPathFromField);
}