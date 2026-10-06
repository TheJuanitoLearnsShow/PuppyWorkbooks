using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Tests;

public sealed class SecretManagerTests
{
    [Fact]
    public void Resolve_EnvSecret_ReturnsValue()
    {
        Environment.SetEnvironmentVariable("TEST_SECRET", "secret_value");
        var config = new SecretManagerConfiguration
        {
            Secrets = [new EnvSecret { SecretName = "MySecret", EnvVarName = "TEST_SECRET" }]
        };
        var manager = new SecretManager(config);
        
        var resolved = manager.Resolve("Value with {{ secrets.MySecret }}");
        
        Assert.Equal("Value with secret_value", resolved);
    }

    [Fact]
    public void IntegrationXmlSerializer_ResolvesSecretsInWorksheet()
    {
        Environment.SetEnvironmentVariable("TEST_SECRET", "resolved_value");
        var xml = @"
<Integration Name='Test'>
  <SecretManager>
    <EnvSecret SecretName='MySecret' EnvVarName='TEST_SECRET' />
  </SecretManager>
  <Steps>
    <Map Id='m1'>
      <Worksheet>
        <Cells>
          <WorkCell>
            <Id>1</Id>
            <Name>Formula</Name>
            <Formula>{{ secrets.MySecret }}</Formula>
          </WorkCell>
        </Cells>
      </Worksheet>
    </Map>
  </Steps>
</Integration>";
        
        var serializer = new IntegrationXmlSerializer();
        var definition = serializer.Deserialize(xml);
        
        var mapStep = definition.Steps.OfType<MapStep>().First();
        Assert.Equal("resolved_value", mapStep.Worksheet.Cells.First().Formula);
    }
    
    [Theory]
    [InlineData("MockSqlOutput.xml")]
    public void IntegrationSchema_TestSecretsInConnectionStrings(string fileName)
    {
      Environment.SetEnvironmentVariable("TEST_SECRET", "secret_value");
      var integrationXmlPath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "Integration", fileName);
      
      var serializer = new IntegrationXmlSerializer();
      var definition = serializer.DeserializeFile(integrationXmlPath);
      
      var sqlInputStep = definition.Steps.OfType<InputStep>().FirstOrDefault(s => s.Kind == InputKind.SqlReader);
        
      Assert.NotNull(sqlInputStep);
      Assert.Equal("Server=secret_value;", sqlInputStep.ConnectionString);
    }
}
