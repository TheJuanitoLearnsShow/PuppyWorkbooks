# PuppyWorkbooks.Integration

Integrations are XML documents containing an input, zero or more map/filter/reduce/switch steps,
and one or more outputs. A worksheet's last non-empty cell is its result.

```xml
<Integration Name="Customers">
  <Steps>
    <IOInput Id="customers" Kind="CSVReader" FilePath="customers.csv" />
    <Map Id="normalize" OutputField="FullName">
      <Worksheet>
        <Name>Normalize</Name>
        <Cells>
          <WorkCell><Id>1</Id><Name>FullName</Name><Formula>FirstName &amp; " " &amp; LastName</Formula></WorkCell>
        </Cells>
      </Worksheet>
    </Map>
    <Filter Id="active">
      <Worksheet>
        <Cells>
          <WorkCell><Id>1</Id><Name>Keep</Name><Formula>Active = "true"</Formula></WorkCell>
        </Cells>
      </Worksheet>
    </Filter>
    <IOOutput Id="archive" Kind="CSVWriter" FilePath="archive.csv" />
  </Steps>
</Integration>
```

```csharp
var definition = new IntegrationXmlSerializer().DeserializeFile("integration.xml");
var result = await new IntegrationRunner().RunAsync(definition);
```

Worksheets may be defined inline, or loaded from a separate worksheet XML file.
The path is resolved relative to the integration file:

```xml
<Map Id="normalize">
  <Worksheet FilePath="worksheets/normalize.xml" />
</Map>
```

`Path`, `File`, `Filename`, and `FileName` are also accepted as aliases for
`FilePath`.

For SQL, provide a vendor-specific ADO.NET connection factory. The library deliberately
does not reference a database vendor package:

```csharp
var runner = new IntegrationRunner(new IntegrationRunnerOptions
{
    ConnectionFactory = cs => new Microsoft.Data.SqlClient.SqlConnection(cs)
});
```

HTTP inputs and outputs can share a named configuration. Register its named clients in
the host's dependency-injection container and provide `HttpClientFactory` to the runner.
`HttpReader` selects object rows using `JsonPath`; `HttpWriter` sends each row as JSON,
XML, or CSV.

```xml
<Integration Name="Customers">
  <HttpConfigurations>
    <HttpConfiguration Name="customerApi" BaseUrl="https://api.example.com/" HttpClientName="customer-api"
                       OAuthClientId="client-id" OAuthClientSecret="secret" OAuthScope="api.read"
                       OAuthTokenUrl="https://identity.example.com/connect/token" OAuthHttpClientName="identity-api">
      <Headers><Header Name="X-Source" Value="PuppyWorkbooks" /></Headers>
    </HttpConfiguration>
  </HttpConfigurations>
  <Steps>
    <IOInput Id="read" Kind="HttpReader" HttpConfiguration="customerApi" Endpoint="customers" JsonPath="$.items" />
    <IOOutput Id="write" Kind="HttpWriter" HttpConfiguration="customerApi" Endpoint="archive" PayloadFormat="Xml" />
  </Steps>
</Integration>
```

Set `ClientCertificateThumbprint` on `HttpConfiguration` to load a client certificate
from the Windows CurrentUser or LocalMachine personal certificate store. JSON paths support
property traversal plus array indexes and wildcards, such as `$.data.items[*]`.

Map, filter, and reduce worksheets receive the current record as the Power Fx record
variable `InputRecord`, so fields can be referenced as `InputRecord.CustomerId`.
Direct input-column variables are also supplied for compatibility. Every formula cell in a map
becomes a field in the output record, keyed by the cell name. `OutputField` is retained for
XML compatibility but is no longer needed by map execution.
Filters keep records when their final value is `true`. Reduce worksheets receive `State`
and the current record, and their final value becomes the configured state field.
Outputs add `<step-id>.Status`, `<step-id>.StatusMessage`, and
`<step-id>.AffectedRows` to the record yielded to subsequent steps.

Switches evaluate their worksheet once per input record. Each branch names a boolean
worksheet cell with `WorkCell`; branches whose cell is true run their nested steps in
declaration order:

```xml
<Switch Id="choose">
  <Worksheet FilePath="worksheets/choose.xml" />
  <Branch WorkCell="IsPreferred">
    <Map Id="preferred"><Worksheet FilePath="worksheets/preferred.xml" /></Map>
  </Branch>
  <Branch WorkCell="IsFallback">
    <Map Id="fallback"><Worksheet FilePath="worksheets/fallback.xml" /></Map>
  </Branch>
</Switch>
```
