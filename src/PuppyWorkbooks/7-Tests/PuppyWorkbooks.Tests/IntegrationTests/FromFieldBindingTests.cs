using System.Collections;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Xml.Linq;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Tests.IntegrationTests;

public sealed class FromFieldBindingTests
{
    [Fact]
    public async Task SqlOutput_BindsFieldAndConstantParametersToStoredProcedure()
    {
        var connection = new RecordingDbConnection();
        await using var provider = new SqlOutputProvider(connection, new SqlOutputStep
        {
            Query = "SaveOrder",
            CommandType = SqlCommandType.StoredProcedure,
            SqlParams =
            [
                new SqlParameterOption { Name = "OrderId", FromField = "Id" },
                new SqlParameterOption { Name = "Mode", FromValue = "5" }
            ]
        });

        await provider.WriteAsync(new IntegrationRecord(new Dictionary<string, object?>
        {
            ["Id"] = 42
        }));

        var command = Assert.IsType<RecordingDbCommand>(connection.LastCommand);
        Assert.Equal("SaveOrder", command.CommandText);
        Assert.Equal(CommandType.StoredProcedure, command.CommandType);
        Assert.Equal(42, command.Parameters[0].Value);
        Assert.Equal("5", command.Parameters[1].Value);
    }

    [Fact]
    public void FromFieldAttributes_RoundTripAndSerializeProperly()
    {
        var serializer = new IntegrationXmlSerializer();
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="FromFieldTest">
              <HttpConfigurations>
                <HttpConfiguration Name="Api" BaseUrlFromField="BaseUrlField" OAuthClientIdFromField="ClientIdField" OAuthClientSecretFromField="SecretField" OAuthScopeFromField="ScopeField" OAuthTokenUrlFromField="TokenUrlField" ClientCertificateThumbprintFromField="CertThumbprintField">
                  <Headers>
                    <Header Name="Authorization" ValueFromField="AuthHeaderField" />
                  </Headers>
                </HttpConfiguration>
              </HttpConfigurations>
              <Steps>
                <CsvInput Id="csvIn" FilePathFromField="CsvInputPathField" />
                <SqlInput Id="sqlIn" ConnectionStringFromField="ConnStrField" QueryFromField="QueryField" TableNameFromField="TableNameField" CommandType="StoredProcedure">
                  <SqlParam Name="CustomerId" FromField="CustomerIdField" />
                  <SqlParam Name="Status" FromValue="5" />
                </SqlInput>
                <HttpInput Id="httpIn" HttpConfiguration="Api" EndpointFromField="EndpointField" JsonPathFromField="JsonPathField" />
                <JsonInput Id="jsonIn" FilePathFromField="JsonInputPathField" JsonPathFromField="JsonPathField" />
                <XmlInput Id="xmlIn" FilePathFromField="XmlInputPathField" XmlItemElementFromField="ItemElemField" />
                <CsvOutput Id="csvOut" FilePathFromField="CsvOutputPathField" />
                <SqlOutput Id="sqlOut" ConnectionStringFromField="ConnStrField" TableNameFromField="TableNameField" QueryFromField="QueryField" CommandType="StoredProcedure">
                  <SqlParam Name="CustomerId" FromField="CustomerIdField" />
                </SqlOutput>
                <HttpOutput Id="httpOut" HttpConfiguration="Api" EndpointFromField="EndpointField" />
                <JsonOutput Id="jsonOut" FilePathFromField="JsonOutputPathField" />
                <XmlOutput Id="xmlOut" FilePathFromField="XmlOutputPathField" XmlRootElementFromField="RootElemField" XmlRecordElementFromField="RecordElemField" />
              </Steps>
            </Integration>
            """;

        var definition = serializer.Deserialize(xml);
        Assert.NotNull(definition);

        var httpConfig = Assert.Single(definition.HttpConfigurations);
        Assert.Equal("BaseUrlField", httpConfig.BaseUrlFromField);
        Assert.Equal("ClientIdField", httpConfig.OAuthClientIdFromField);
        Assert.Equal("SecretField", httpConfig.OAuthClientSecretFromField);
        Assert.Equal("ScopeField", httpConfig.OAuthScopeFromField);
        Assert.Equal("TokenUrlField", httpConfig.OAuthTokenUrlFromField);
        Assert.Equal("CertThumbprintField", httpConfig.ClientCertificateThumbprintFromField);
        Assert.Equal("AuthHeaderField", Assert.Single(httpConfig.Headers).ValueFromField);

        var steps = definition.Steps;
        var csvIn = Assert.IsType<CsvInputStep>(steps[0]);
        Assert.Equal("CsvInputPathField", csvIn.FilePathFromField);

        var sqlIn = Assert.IsType<SqlInputStep>(steps[1]);
        Assert.Equal("ConnStrField", sqlIn.ConnectionStringFromField);
        Assert.Equal("QueryField", sqlIn.QueryFromField);
        Assert.Equal("TableNameField", sqlIn.TableNameFromField);
        Assert.Equal(SqlCommandType.StoredProcedure, sqlIn.CommandType);
        Assert.Equal("CustomerIdField", sqlIn.SqlParams[0].FromField);
        Assert.Equal("5", sqlIn.SqlParams[1].FromValue);

        var httpIn = Assert.IsType<HttpInputStep>(steps[2]);
        Assert.Equal("EndpointField", httpIn.EndpointFromField);
        Assert.Equal("JsonPathField", httpIn.JsonPathFromField);

        var jsonIn = Assert.IsType<JsonInputStep>(steps[3]);
        Assert.Equal("JsonInputPathField", jsonIn.FilePathFromField);
        Assert.Equal("JsonPathField", jsonIn.JsonPathFromField);

        var xmlIn = Assert.IsType<XmlInputStep>(steps[4]);
        Assert.Equal("XmlInputPathField", xmlIn.FilePathFromField);
        Assert.Equal("ItemElemField", xmlIn.XmlItemElementFromField);

        var csvOut = Assert.IsType<CsvOutputStep>(steps[5]);
        Assert.Equal("CsvOutputPathField", csvOut.FilePathFromField);

        var sqlOut = Assert.IsType<SqlOutputStep>(steps[6]);
        Assert.Equal("ConnStrField", sqlOut.ConnectionStringFromField);
        Assert.Equal("TableNameField", sqlOut.TableNameFromField);
        Assert.Equal("QueryField", sqlOut.QueryFromField);
        Assert.Equal(SqlCommandType.StoredProcedure, sqlOut.CommandType);
        Assert.Equal("CustomerIdField", Assert.Single(sqlOut.SqlParams).FromField);

        var httpOut = Assert.IsType<HttpOutputStep>(steps[7]);
        Assert.Equal("EndpointField", httpOut.EndpointFromField);

        var jsonOut = Assert.IsType<JsonOutputStep>(steps[8]);
        Assert.Equal("JsonOutputPathField", jsonOut.FilePathFromField);

        var xmlOut = Assert.IsType<XmlOutputStep>(steps[9]);
        Assert.Equal("XmlOutputPathField", xmlOut.FilePathFromField);
        Assert.Equal("RootElemField", xmlOut.XmlRootElementFromField);
        Assert.Equal("RecordElemField", xmlOut.XmlRecordElementFromField);

        var serializedXml = serializer.Serialize(definition);
        var doc = XDocument.Parse(serializedXml);
        var serializedSteps = doc.Root!.Element("Steps")!.Elements().ToList();

        Assert.Equal("CsvInputPathField", (string?)serializedSteps[0].Attribute("FilePathFromField"));
        Assert.Equal("ConnStrField", (string?)serializedSteps[1].Attribute("ConnectionStringFromField"));
        Assert.Equal("QueryField", (string?)serializedSteps[1].Attribute("QueryFromField"));
        Assert.Equal("TableNameField", (string?)serializedSteps[1].Attribute("TableNameFromField"));
        Assert.Equal("StoredProcedure", (string?)serializedSteps[1].Attribute("CommandType"));
        Assert.Equal("CustomerIdField", (string?)serializedSteps[1].Element("SqlParam")?.Attribute("FromField"));
        Assert.Equal("5", (string?)serializedSteps[1].Elements("SqlParam").ElementAt(1).Attribute("FromValue"));
        Assert.Equal("EndpointField", (string?)serializedSteps[2].Attribute("EndpointFromField"));
        Assert.Equal("JsonPathField", (string?)serializedSteps[2].Attribute("JsonPathFromField"));
        Assert.Equal("JsonInputPathField", (string?)serializedSteps[3].Attribute("FilePathFromField"));
        Assert.Equal("XmlInputPathField", (string?)serializedSteps[4].Attribute("FilePathFromField"));
        Assert.Equal("ItemElemField", (string?)serializedSteps[4].Attribute("XmlItemElementFromField"));
        Assert.Equal("CsvOutputPathField", (string?)serializedSteps[5].Attribute("FilePathFromField"));
        Assert.Equal("ConnStrField", (string?)serializedSteps[6].Attribute("ConnectionStringFromField"));
        Assert.Equal("TableNameField", (string?)serializedSteps[6].Attribute("TableNameFromField"));
        Assert.Equal("QueryField", (string?)serializedSteps[6].Attribute("QueryFromField"));
        Assert.Equal("StoredProcedure", (string?)serializedSteps[6].Attribute("CommandType"));
        Assert.Equal("CustomerIdField", (string?)serializedSteps[6].Element("SqlParam")?.Attribute("FromField"));
        Assert.Equal("EndpointField", (string?)serializedSteps[7].Attribute("EndpointFromField"));
        Assert.Equal("JsonOutputPathField", (string?)serializedSteps[8].Attribute("FilePathFromField"));
        Assert.Equal("XmlOutputPathField", (string?)serializedSteps[9].Attribute("FilePathFromField"));
        Assert.Equal("RootElemField", (string?)serializedSteps[9].Attribute("XmlRootElementFromField"));
        Assert.Equal("RecordElemField", (string?)serializedSteps[9].Attribute("XmlRecordElementFromField"));
    }

    [Fact]
    public async Task XmlOutput_InjectsFilePathAndElementsFromInputRow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputCsvPath = Path.Combine(tempDir, "input.csv");
        var outputXmlPath = Path.Combine(tempDir, "output.xml");

        try
        {
            await File.WriteAllTextAsync(inputCsvPath, $"Id,Name,XmlPath{Environment.NewLine}101,First,{outputXmlPath}{Environment.NewLine}102,Second,{outputXmlPath}");

            var xml = $"""
                <Integration Name="XmlFromFieldTest">
                  <Steps>
                    <CsvInput Id="csvIn" FilePath="{inputCsvPath.Replace('\\', '/')}" />
                    <XmlOutput Id="xml" FilePathFromField="XmlPath" XmlRootElement="Items" XmlRecordElement="Item" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(2, result.Written);
            Assert.True(File.Exists(outputXmlPath));

            var doc = XDocument.Load(outputXmlPath);
            Assert.Equal("Items", doc.Root!.Name.LocalName);
            var items = doc.Root.Elements("Item").ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("101", items[0].Element("Id")?.Value);
            Assert.Equal("First", items[0].Element("Name")?.Value);
            Assert.Equal("102", items[1].Element("Id")?.Value);
            Assert.Equal("Second", items[1].Element("Name")?.Value);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CsvAndJsonOutput_InjectsFilePathFromInputRow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputCsvPath = Path.Combine(tempDir, "input.csv");
        var outputCsvPath = Path.Combine(tempDir, "output.csv");
        var outputJsonPath = Path.Combine(tempDir, "output.json");

        try
        {
            await File.WriteAllTextAsync(inputCsvPath, $"Id,Name,CsvTarget,JsonTarget{Environment.NewLine}1,Alpha,{outputCsvPath},{outputJsonPath}{Environment.NewLine}2,Beta,{outputCsvPath},{outputJsonPath}");

            var xml = $"""
                <Integration Name="OutputFromFieldTest">
                  <Steps>
                    <CsvInput Id="csvIn" FilePath="{inputCsvPath.Replace('\\', '/')}" />
                    <CsvOutput Id="csvOut" FilePathFromField="CsvTarget" />
                    <JsonOutput Id="jsonOut" FilePathFromField="JsonTarget" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(4, result.Written);
            Assert.True(File.Exists(outputCsvPath));
            Assert.True(File.Exists(outputJsonPath));

            var csvLines = await File.ReadAllLinesAsync(outputCsvPath);
            Assert.Equal(3, csvLines.Length); // header + 2 rows
            Assert.Contains("Alpha", csvLines[1]);
            Assert.Contains("Beta", csvLines[2]);

            var jsonText = await File.ReadAllTextAsync(outputJsonPath);
            Assert.Contains("Alpha", jsonText);
            Assert.Contains("Beta", jsonText);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HttpOutput_And_HttpConfiguration_InjectsValuesFromInputRow()
    {
        var handler = new TestHttpHandler();
        var tempDir = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputCsvPath = Path.Combine(tempDir, "input.csv");

        try
        {
            await File.WriteAllTextAsync(inputCsvPath, $"Id,Name,BaseUrlField,EndpointField,TokenField{Environment.NewLine}1,Ada,https://test.local/v1/,upload-user,Bearer Token123{Environment.NewLine}2,Bob,https://test.local/v1/,upload-user,Bearer Token456");

            var xml = $"""
                <Integration Name="HttpFromFieldTest">
                  <HttpConfigurations>
                    <HttpConfiguration Name="DynamicApi" BaseUrlFromField="BaseUrlField" HttpClientName="test-client">
                      <Headers>
                        <Header Name="Authorization" ValueFromField="TokenField" />
                      </Headers>
                    </HttpConfiguration>
                  </HttpConfigurations>
                  <Steps>
                    <CsvInput Id="csvIn" FilePath="{inputCsvPath.Replace('\\', '/')}" />
                    <HttpOutput Id="httpOut" HttpConfiguration="DynamicApi" EndpointFromField="EndpointField" PayloadFormat="Json" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                HttpClientFactory = new SimpleHttpClientFactory(handler, "test-client")
            });

            var result = await runner.RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(2, result.Written);
            Assert.Equal(2, handler.SentRequests.Count);

            Assert.Equal("https://test.local/v1/upload-user", handler.SentRequests[0].Uri.ToString());
            Assert.Equal("Bearer Token123", handler.SentRequests[0].AuthHeader);
            Assert.Contains("Ada", handler.SentRequests[0].Body);

            Assert.Equal("https://test.local/v1/upload-user", handler.SentRequests[1].Uri.ToString());
            Assert.Equal("Bearer Token456", handler.SentRequests[1].AuthHeader);
            Assert.Contains("Bob", handler.SentRequests[1].Body);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ChildInputProvider_InjectsFilePathFromInputRow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var parentCsvPath = Path.Combine(tempDir, "parent.csv");
        var childCsvPath1 = Path.Combine(tempDir, "child1.csv");
        var childCsvPath2 = Path.Combine(tempDir, "child2.csv");
        var outputCsvPath = Path.Combine(tempDir, "output.csv");

        try
        {
            await File.WriteAllTextAsync(parentCsvPath, $"ParentId,ChildFile{Environment.NewLine}P1,{childCsvPath1}{Environment.NewLine}P2,{childCsvPath2}");
            await File.WriteAllTextAsync(childCsvPath1, $"ChildVal{Environment.NewLine}C1-A{Environment.NewLine}C1-B");
            await File.WriteAllTextAsync(childCsvPath2, $"ChildVal{Environment.NewLine}C2-A");

            var xml = $"""
                <Integration Name="ChildInputTest">
                  <Steps>
                    <CsvInput Id="parentIn" FilePath="{parentCsvPath.Replace('\\', '/')}" />
                    <CsvInput Id="childIn" FilePathFromField="ChildFile" />
                    <CsvOutput Id="sink" FilePath="{outputCsvPath.Replace('\\', '/')}" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var result = await new IntegrationRunner().RunAsync(definition);

            Assert.Equal(2, result.Read);
            Assert.Equal(3, result.Written);

            var lines = await File.ReadAllLinesAsync(outputCsvPath);
            Assert.Equal(4, lines.Length);
            Assert.Contains("C1-A", lines[1]);
            Assert.Contains("C1-B", lines[2]);
            Assert.Contains("C2-A", lines[3]);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FromFieldXml_ValidatesAgainstSchema()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", "Integration.xsd");
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Integration Name="FromFieldSchemaValidationTest">
              <HttpConfigurations>
                <HttpConfiguration Name="Api" BaseUrlFromField="BaseUrlField" OAuthClientIdFromField="ClientIdField" OAuthClientSecretFromField="SecretField" OAuthScopeFromField="ScopeField" OAuthTokenUrlFromField="TokenUrlField" ClientCertificateThumbprintFromField="CertThumbprintField">
                  <Headers>
                    <Header Name="Authorization" ValueFromField="AuthHeaderField" />
                  </Headers>
                </HttpConfiguration>
              </HttpConfigurations>
              <Steps>
                <CsvInput Id="csvIn" FilePathFromField="CsvInputPathField" />
                <SqlInput Id="sqlIn" ConnectionStringFromField="ConnStrField" QueryFromField="QueryField" TableNameFromField="TableNameField" CommandType="StoredProcedure">
                  <SqlParam Name="CustomerId" FromField="CustomerIdField" />
                  <SqlParam Name="Status" FromValue="5" />
                </SqlInput>
                <HttpInput Id="httpIn" HttpConfiguration="Api" EndpointFromField="EndpointField" JsonPathFromField="JsonPathField" />
                <JsonInput Id="jsonIn" FilePathFromField="JsonInputPathField" JsonPathFromField="JsonPathField" />
                <XmlInput Id="xmlIn" FilePathFromField="XmlInputPathField" XmlItemElementFromField="ItemElemField" />
                <CsvOutput Id="csvOut" FilePathFromField="CsvOutputPathField" />
                <SqlOutput Id="sqlOut" ConnectionStringFromField="ConnStrField" TableNameFromField="TableNameField" QueryFromField="QueryField" CommandType="Text">
                  <SqlParam Name="CustomerId" FromField="CustomerIdField" />
                </SqlOutput>
                <HttpOutput Id="httpOut" HttpConfiguration="Api" EndpointFromField="EndpointField" />
                <JsonOutput Id="jsonOut" FilePathFromField="JsonOutputPathField" />
                <XmlOutput Id="xmlOut" FilePathFromField="XmlOutputPathField" XmlRootElementFromField="RootElemField" XmlRecordElementFromField="RecordElemField" />
              </Steps>
            </Integration>
            """;

        var schemas = new System.Xml.Schema.XmlSchemaSet();
        schemas.Add(null, schemaPath);

        var settings = new System.Xml.XmlReaderSettings
        {
            ValidationType = System.Xml.ValidationType.Schema,
            Schemas = schemas,
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema |
                              System.Xml.Schema.XmlSchemaValidationFlags.ProcessSchemaLocation |
                              System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings
        };

        var errors = new List<string>();
        settings.ValidationEventHandler += (_, args) =>
        {
            if (args.Severity == System.Xml.Schema.XmlSeverityType.Error)
            {
                errors.Add($"Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}: {args.Message}");
            }
        };

        using var stringReader = new StringReader(xml);
        using var reader = System.Xml.XmlReader.Create(stringReader, settings);
        while (reader.Read()) { }

        Assert.Empty(errors);
    }

    [Fact]
    public async Task MissingField_ThrowsInvalidOperationException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PuppyWorkbooksTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputCsvPath = Path.Combine(tempDir, "input.csv");

        try
        {
            await File.WriteAllTextAsync(inputCsvPath, $"Id,Name{Environment.NewLine}1,Test");

            var xml = $"""
                <Integration Name="MissingFieldTest">
                  <Steps>
                    <CsvInput Id="csvIn" FilePath="{inputCsvPath.Replace('\\', '/')}" />
                    <XmlOutput Id="xml" FilePathFromField="NonExistentField" XmlRootElement="Items" XmlRecordElement="Item" />
                  </Steps>
                </Integration>
                """;

            var definition = new IntegrationXmlSerializer().Deserialize(xml);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new IntegrationRunner().RunAsync(definition));
            Assert.Contains("NonExistentField", ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    private sealed class SimpleHttpClientFactory(HttpMessageHandler handler, string expectedName) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(expectedName, name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class TestHttpHandler : HttpMessageHandler
    {
        public List<(Uri Uri, string? AuthHeader, string Body)> SentRequests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is not null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
            var auth = request.Headers.TryGetValues("Authorization", out var values) ? string.Join(",", values) : null;
            SentRequests.Add((request.RequestUri!, auth, body ?? string.Empty));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    private sealed class RecordingDbConnection : DbConnection
    {
        private ConnectionState _state;

        public RecordingDbCommand? LastCommand { get; private set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "1";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand()
        {
            LastCommand = new RecordingDbCommand { Connection = this };
            return LastCommand;
        }
    }

    private sealed class RecordingDbCommand : DbCommand
    {
        private readonly RecordingDbParameterCollection _parameters = new();

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 1;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new RecordingDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class RecordingDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;
        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }
        public override void AddRange(Array values)
        {
            foreach (var value in values) Add(value!);
        }
        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0) _parameters.Add(value);
            else _parameters[index] = value;
        }
    }
}
