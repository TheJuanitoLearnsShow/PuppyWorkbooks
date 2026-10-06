using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;

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
}
