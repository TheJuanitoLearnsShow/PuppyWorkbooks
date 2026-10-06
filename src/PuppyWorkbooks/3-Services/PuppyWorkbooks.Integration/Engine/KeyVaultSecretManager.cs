using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class KeyVaultSecretManager
{
    private readonly Dictionary<string, SecretClient> _kvClients = new(StringComparer.OrdinalIgnoreCase);

    public string GetKeyVaultSecret(KeyVaultCredential kv, string secretName)
    {
        if (!_kvClients.TryGetValue(kv.KeyVaultURI, out var client))
        {
            client = new SecretClient(new Uri(kv.KeyVaultURI), new DefaultAzureCredential());
            _kvClients[kv.KeyVaultURI] = client;
        }

        var secret = client.GetSecret(secretName);
        return secret.Value.Value ?? throw new InvalidOperationException($"KeyVault secret '{secretName}' has no value.");
    }
}