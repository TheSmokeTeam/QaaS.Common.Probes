using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.RabbitMqProbes;

public abstract class BaseRabbitMqManagementProbe<TRabbitMqManagementConfig> : BaseProbe<TRabbitMqManagementConfig>
    where TRabbitMqManagementConfig : BaseRabbitMqManagementConfig, new()
{
    protected string ManagementApiBaseUrl = string.Empty;

    protected virtual HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        if (Configuration.AllowInvalidServerCertificates)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = BuildManagementApiBaseUri(),
            Timeout = TimeSpan.FromMilliseconds(Configuration.RequestTimeoutMs)
        };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{Configuration.Username}:{Configuration.Password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        using var httpClient = CreateHttpClient();
        ManagementApiBaseUrl = httpClient.BaseAddress?.ToString() ?? string.Empty;
        RunRabbitMqManagementProbe(httpClient);
        Context.Logger.LogInformation("Performed rabbitmq management action {ActionType} against {ManagementApiBaseUrl}",
            GetType().Name, ManagementApiBaseUrl);
    }

    protected abstract void RunRabbitMqManagementProbe(HttpClient httpClient);

    protected async Task<string> SendManagementRequestAsync(HttpClient httpClient, HttpMethod method, string relativePath,
        string? jsonPayload = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, relativePath);
        if (jsonPayload is not null)
        {
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"RabbitMQ management request to {request.RequestUri} failed with status code {(int)response.StatusCode}: {responseContent}");
        }

        return responseContent;
    }

    protected static string EncodePathSegment(string value) => Uri.EscapeDataString(value);

    private Uri BuildManagementApiBaseUri()
        => new UriBuilder(Configuration.ManagementScheme, Configuration.Host!, Configuration.ManagementPort, "api/")
            .Uri;
}
