using System.Text.RegularExpressions;
using AdysTech.CredentialManager;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class SecretManager
{
    private static readonly Regex SecretPattern = new(
        @"\{\{\s*secrets\.([\w.]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private readonly SecretManagerConfiguration? _configuration;
    private readonly KeyVaultSecretManager _keyVaultSecretManager;

    public SecretManager(SecretManagerConfiguration? configuration)
    {
        _configuration = configuration;
        _keyVaultSecretManager = new KeyVaultSecretManager();
    }

    public string Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
        
        return SecretPattern.Replace(value, match =>
        {
            var secretName = match.Groups[1].Value;
            return GetSecretValue(secretName);
        });
    }

    private string GetSecretValue(string secretName)
    {
        var secretDef = _configuration?.Secrets.FirstOrDefault(s => string.Equals(s.SecretName, secretName, StringComparison.OrdinalIgnoreCase));
        if (secretDef is null)
            throw new InvalidOperationException($"Secret '{secretName}' is not defined in the integration configuration.");

        return secretDef switch
        {
            EnvSecret env => Environment.GetEnvironmentVariable(env.EnvVarName) ?? throw new InvalidOperationException($"Environment variable '{env.EnvVarName}' not found for secret '{secretName}'."),
            WindowsCredentialsSecret win => WindowsCredentialStoreSecretManager.GetWindowsCredential(win, secretName),
            KeyVaultCredential kv => _keyVaultSecretManager.GetKeyVaultSecret(kv, secretName),
            _ => throw new NotSupportedException($"Secret provider type '{secretDef.GetType().Name}' is not supported.")
        };
    }
}
