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
    
    public static void ResolveSecrets(IntegrationDefinition definition)
    {
        var secretManager = new SecretManager(definition.SecretManager);

        foreach (var httpConfig in definition.HttpConfigurations)
        {
            httpConfig.BaseUrl = secretManager.Resolve(httpConfig.BaseUrl);
            httpConfig.BaseUrlFromField = secretManager.Resolve(httpConfig.BaseUrlFromField);
            httpConfig.OAuthClientId = secretManager.Resolve(httpConfig.OAuthClientId);
            httpConfig.OAuthClientIdFromField = secretManager.Resolve(httpConfig.OAuthClientIdFromField);
            httpConfig.OAuthClientSecret = secretManager.Resolve(httpConfig.OAuthClientSecret);
            httpConfig.OAuthClientSecretFromField = secretManager.Resolve(httpConfig.OAuthClientSecretFromField);
            httpConfig.OAuthScope = secretManager.Resolve(httpConfig.OAuthScope);
            httpConfig.OAuthScopeFromField = secretManager.Resolve(httpConfig.OAuthScopeFromField);
            httpConfig.OAuthTokenUrl = secretManager.Resolve(httpConfig.OAuthTokenUrl);
            httpConfig.OAuthTokenUrlFromField = secretManager.Resolve(httpConfig.OAuthTokenUrlFromField);
            httpConfig.ClientCertificateThumbprint = secretManager.Resolve(httpConfig.ClientCertificateThumbprint);
            httpConfig.ClientCertificateThumbprintFromField = secretManager.Resolve(httpConfig.ClientCertificateThumbprintFromField);
            foreach (var header in httpConfig.Headers)
            {
                header.Value = secretManager.Resolve(header.Value);
                header.ValueFromField = secretManager.Resolve(header.ValueFromField);
            }
        }

        ResolveSecretsInSteps(definition.Steps, secretManager);
    }

    private static void ResolveSecretsInSteps(IEnumerable<IntegrationStep> steps, SecretManager secretManager)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case MapStep map:
                    ResolveSecretsInWorksheet(map.Worksheet, secretManager);
                    break;
                case FilterStep filter:
                    ResolveSecretsInWorksheet(filter.Worksheet, secretManager);
                    break;
                case ReduceStep reduce:
                    ResolveSecretsInWorksheet(reduce.Worksheet, secretManager);
                    break;
                case SwitchStep @switch:
                    ResolveSecretsInWorksheet(@switch.Worksheet, secretManager);
                    foreach (var branch in @switch.Branches)
                    {
                        ResolveSecretsInSteps(branch.Steps, secretManager);
                    }
                    break;
                case CsvInputProviderOptions csvIn:
                    csvIn.FilePath = secretManager.Resolve(csvIn.FilePath);
                    csvIn.FilePathFromField = secretManager.Resolve(csvIn.FilePathFromField);
                    ResolveInputMocks(csvIn, secretManager);
                    break;
                case SqlInputProviderOptions sqlIn:
                    sqlIn.ConnectionString = secretManager.Resolve(sqlIn.ConnectionString);
                    sqlIn.ConnectionStringFromField = secretManager.Resolve(sqlIn.ConnectionStringFromField);
                    sqlIn.TableName = secretManager.Resolve(sqlIn.TableName);
                    sqlIn.TableNameFromField = secretManager.Resolve(sqlIn.TableNameFromField);
                    sqlIn.Query = secretManager.Resolve(sqlIn.Query);
                    sqlIn.QueryFromField = secretManager.Resolve(sqlIn.QueryFromField);
                    ResolveInputMocks(sqlIn, secretManager);
                    break;
                case HttpInputProviderOptions httpIn:
                    httpIn.Endpoint = secretManager.Resolve(httpIn.Endpoint);
                    httpIn.EndpointFromField = secretManager.Resolve(httpIn.EndpointFromField);
                    httpIn.HttpMethod = secretManager.Resolve(httpIn.HttpMethod);
                    httpIn.JsonPath = secretManager.Resolve(httpIn.JsonPath);
                    httpIn.JsonPathFromField = secretManager.Resolve(httpIn.JsonPathFromField);
                    ResolveInputMocks(httpIn, secretManager);
                    break;
                case JsonInputProviderOptions jsonIn:
                    jsonIn.FilePath = secretManager.Resolve(jsonIn.FilePath);
                    jsonIn.FilePathFromField = secretManager.Resolve(jsonIn.FilePathFromField);
                    jsonIn.JsonPath = secretManager.Resolve(jsonIn.JsonPath);
                    jsonIn.JsonPathFromField = secretManager.Resolve(jsonIn.JsonPathFromField);
                    ResolveInputMocks(jsonIn, secretManager);
                    break;
                case XmlInputProviderOptions xmlIn:
                    xmlIn.FilePath = secretManager.Resolve(xmlIn.FilePath);
                    xmlIn.FilePathFromField = secretManager.Resolve(xmlIn.FilePathFromField);
                    xmlIn.XmlItemElement = secretManager.Resolve(xmlIn.XmlItemElement);
                    xmlIn.XmlItemElementFromField = secretManager.Resolve(xmlIn.XmlItemElementFromField);
                    ResolveInputMocks(xmlIn, secretManager);
                    break;
                case FileSystemInputProviderOptions fsIn:
                    fsIn.FilePath = secretManager.Resolve(fsIn.FilePath);
                    fsIn.FilePathFromField = secretManager.Resolve(fsIn.FilePathFromField);
                    ResolveInputMocks(fsIn, secretManager);
                    break;
                case MemoryInputProviderOptions memIn:
                    ResolveInputMocks(memIn, secretManager);
                    break;
                case InputStep input:
                    ResolveInputMocks(input, secretManager);
                    break;
                case CsvOutputProviderOptions csvOut:
                    csvOut.FilePath = secretManager.Resolve(csvOut.FilePath);
                    csvOut.FilePathFromField = secretManager.Resolve(csvOut.FilePathFromField);
                    break;
                case SqlOutputProviderOptions sqlOut:
                    sqlOut.ConnectionString = secretManager.Resolve(sqlOut.ConnectionString);
                    sqlOut.ConnectionStringFromField = secretManager.Resolve(sqlOut.ConnectionStringFromField);
                    sqlOut.TableName = secretManager.Resolve(sqlOut.TableName);
                    sqlOut.TableNameFromField = secretManager.Resolve(sqlOut.TableNameFromField);
                    sqlOut.Query = secretManager.Resolve(sqlOut.Query);
                    sqlOut.QueryFromField = secretManager.Resolve(sqlOut.QueryFromField);
                    break;
                case HttpOutputProviderOptions httpOut:
                    httpOut.Endpoint = secretManager.Resolve(httpOut.Endpoint);
                    httpOut.EndpointFromField = secretManager.Resolve(httpOut.EndpointFromField);
                    httpOut.HttpMethod = secretManager.Resolve(httpOut.HttpMethod);
                    break;
                case JsonOutputProviderOptions jsonOut:
                    jsonOut.FilePath = secretManager.Resolve(jsonOut.FilePath);
                    jsonOut.FilePathFromField = secretManager.Resolve(jsonOut.FilePathFromField);
                    break;
                case XmlOutputProviderOptions xmlOut:
                    xmlOut.FilePath = secretManager.Resolve(xmlOut.FilePath);
                    xmlOut.FilePathFromField = secretManager.Resolve(xmlOut.FilePathFromField);
                    xmlOut.XmlRootElement = secretManager.Resolve(xmlOut.XmlRootElement);
                    xmlOut.XmlRootElementFromField = secretManager.Resolve(xmlOut.XmlRootElementFromField);
                    xmlOut.XmlRecordElement = secretManager.Resolve(xmlOut.XmlRecordElement);
                    xmlOut.XmlRecordElementFromField = secretManager.Resolve(xmlOut.XmlRecordElementFromField);
                    break;
            }
        }
    }

    private static void ResolveInputMocks(InputStep input, SecretManager secretManager)
    {
        input.MockCsvFilePath = secretManager.Resolve(input.MockCsvFilePath);
        input.MockCsv = secretManager.Resolve(input.MockCsv);
        input.MockData = secretManager.Resolve(input.MockData);
        foreach (var dataSource in input.MockDataSources.Values)
        {
            dataSource.FilePath = secretManager.Resolve(dataSource.FilePath);
            dataSource.Content = secretManager.Resolve(dataSource.Content);
        }
    }

    private static void ResolveSecretsInWorksheet(WorkSheet? worksheet, SecretManager secretManager)
    {
        if (worksheet == null) return;
        foreach (var cell in worksheet.Cells)
        {
            cell.Formula = secretManager.Resolve(cell.Formula);
        }
    }
}
