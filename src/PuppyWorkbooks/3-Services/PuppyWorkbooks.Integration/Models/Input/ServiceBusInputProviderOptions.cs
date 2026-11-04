using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class ServiceBusInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.ServiceBusReader;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionStringFromField { get; set; } = string.Empty;
    [XmlAttribute] public string TopicName { get; set; } = string.Empty;
    [XmlAttribute] public string TopicNameFromField { get; set; } = string.Empty;
    [XmlAttribute] public string SubscriptionName { get; set; } = string.Empty;
    [XmlAttribute] public string SubscriptionNameFromField { get; set; } = string.Empty;

    public bool ShouldSerializeConnectionString() => !string.IsNullOrWhiteSpace(ConnectionString);
    public bool ShouldSerializeConnectionStringFromField() => !string.IsNullOrWhiteSpace(ConnectionStringFromField);
    public bool ShouldSerializeTopicName() => !string.IsNullOrWhiteSpace(TopicName);
    public bool ShouldSerializeTopicNameFromField() => !string.IsNullOrWhiteSpace(TopicNameFromField);
    public bool ShouldSerializeSubscriptionName() => !string.IsNullOrWhiteSpace(SubscriptionName);
    public bool ShouldSerializeSubscriptionNameFromField() => !string.IsNullOrWhiteSpace(SubscriptionNameFromField);
}