using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public abstract class HttpProviderBase : IAsyncDisposable
{
    private readonly HttpProviderSettings _settings;
    private readonly IHttpClientFactory? _httpClientFactory;
    private HttpClient? _client;
    private bool _ownsClient;

    protected HttpProviderBase(HttpProviderSettings settings, IHttpClientFactory? httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, HttpContent? content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, CreateRequestUri(endpoint)) { Content = content };
        ApplyHeaders(request);
        var token = await GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await GetClient().SendAsync(request, cancellationToken);
    }

    private Uri CreateRequestUri(string endpoint)
    {
        if (!Uri.TryCreate(_settings.BaseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("HTTP configuration requires an absolute BaseUrl.");
        return string.IsNullOrWhiteSpace(endpoint) ? baseUri : new Uri(baseUri, endpoint);
    }

    private HttpClient GetClient()
    {
        if (_client is not null) return _client;
        if (!string.IsNullOrWhiteSpace(_settings.ClientCertificateThumbprint))
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(FindCertificate(_settings.ClientCertificateThumbprint));
            _client = new HttpClient(handler, disposeHandler: true);
            _ownsClient = true;
            return _client;
        }
        _client = _httpClientFactory?.CreateClient(_settings.HttpClientName) ?? new HttpClient();
        _ownsClient = _httpClientFactory is null;
        return _client;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.OAuthTokenUrl)) return null;
        if (string.IsNullOrWhiteSpace(_settings.OAuthClientId) || string.IsNullOrWhiteSpace(_settings.OAuthClientSecret))
            throw new InvalidOperationException("OAuthTokenUrl requires OAuthClientId and OAuthClientSecret.");

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.OAuthTokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _settings.OAuthClientId,
                ["client_secret"] = _settings.OAuthClientSecret,
                ["scope"] = _settings.OAuthScope
            })
        };
        ApplyHeaders(request);
        var tokenClient = _httpClientFactory?.CreateClient(_settings.OAuthHttpClientName) ?? new HttpClient();
        try
        {
            using var response = await tokenClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            return document.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("OAuth token response did not contain access_token.");
        }
        finally
        {
            if (_httpClientFactory is null) tokenClient.Dispose();
        }
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        foreach (var header in _settings.Headers.Where(header => !string.IsNullOrWhiteSpace(header.Name)))
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);
    }

    private static X509Certificate2 FindCertificate(string thumbprint)
    {
        var normalizedThumbprint = thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal);
        foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false)
                .OfType<X509Certificate2>().FirstOrDefault();
            if (certificate is not null) return certificate;
        }
        throw new InvalidOperationException($"Client certificate '{thumbprint}' was not found in the Windows certificate store.");
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsClient) _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
