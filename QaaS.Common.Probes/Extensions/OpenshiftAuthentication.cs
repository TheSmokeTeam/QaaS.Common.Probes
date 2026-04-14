using System.Net;
using System.Net.Http.Headers;
using System.Text;
using k8s;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QaaS.Common.Probes.Extensions;

/// <summary>
/// Extension methods for the <see cref="Kubernetes"/> class that provides Openshift authentication with ease.
/// </summary>
public static class OpenshiftAuthentication
{
    /// <summary>
    /// Get the authorization endpoint of the Openshift cluster.
    /// This endpoint leads to the cluster's required url through which the access token of a user is generated.
    /// </summary>
    /// <param name="host">The url of the openshift cluster</param>
    /// <param name="allowInvalidServerCertificates">Whether TLS certificate validation should be skipped while discovering the OAuth endpoint.</param>
    /// <returns>The auth url.</returns>
    private static string DiscoverAuthorizationEndpoint(string host, bool allowInvalidServerCertificates)
    {
        if (host[^1] == '/')
            host = host.Remove(host.Length - 1);

        var url = $"{host}/.well-known/oauth-authorization-server";
        using var discoveryHttpClient = CreateHttpClient(allowInvalidServerCertificates);
        using var response = discoveryHttpClient.GetAsync(url).GetAwaiter().GetResult();
        var oauthInfo = ReadSuccessfulResponseContent(response, $"OpenShift OAuth discovery request to {url}");
        var jsonAuthInfo = JObject.Load(new JsonTextReader(new StringReader(oauthInfo)));
        return jsonAuthInfo["authorization_endpoint"]?.ToString()
               ?? throw new InvalidOperationException(
                   $"OpenShift OAuth discovery response from {url} did not contain an authorization endpoint.");
    }

    /// <summary>
    /// Get the access token of the given username and password to the given Openshift cluster.
    /// </summary>
    /// <param name="host">The url of the openshift cluster</param>
    /// <param name="username">The username to authenticate.</param>
    /// <param name="password">The password of the username to authenticate.</param>
    /// <param name="allowInvalidServerCertificates">Whether TLS certificate validation should be skipped for the discovery and authorization HTTP calls.</param>
    /// <returns>An access token to the Openshift cluster.</returns>
    private static string GetToken(string host, string username, string password, bool allowInvalidServerCertificates)
    {
        var authorizationEndpoint = DiscoverAuthorizationEndpoint(host, allowInvalidServerCertificates);
        var authUrl =
            $"{authorizationEndpoint}?response_type=token&client_id=openshift-challenging-client";

        var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, authUrl);
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        httpRequest.Headers.Add("X-Csrf-Token", "1");

        using var authorizationHttpClient = CreateHttpClient(allowInvalidServerCertificates, allowAutoRedirect: false);
        using var authorizationResponse = authorizationHttpClient.SendAsync(httpRequest).GetAwaiter().GetResult();
        return ExtractAccessTokenFromAuthorizationResponse(authorizationResponse);
    }

    /// <summary>
    /// Login to an Openshift cluster via a <see cref="Kubernetes"/> object.
    /// This method authenticates the kubernetes client object by assigning the access token to the
    /// configured host to the client's <see cref="KubernetesClientConfiguration"/>.
    /// The host to which the method will login to is the one configured in the client's configuration (which
    /// is configured when a <see cref="Kubernetes"/> object is instantiated.
    /// </summary>
    /// <param name="cluster">The cluster to authenticate to</param>
    /// <param name="username">The username to authenticate.</param>
    /// <param name="password">The password of the username to authenticate.</param>
    /// <param name="allowInvalidServerCertificates">Whether TLS certificate validation should be skipped for this cluster connection.</param>
    /// <returns>An authenticated <see cref="Kubernetes"/> client configured for the requested cluster.</returns>
    public static Kubernetes CreateKubernetesClient(string cluster, string username, string password,
        bool allowInvalidServerCertificates = true)
    {
        if (!cluster.StartsWith("http"))
            cluster = $"https://{cluster}";

        var k8SClientConfig = new KubernetesClientConfiguration
            { Host = cluster, SkipTlsVerify = allowInvalidServerCertificates };
        var k8SClientWithNoToken = new Kubernetes(k8SClientConfig);
        var accessToken = GetToken(k8SClientWithNoToken.BaseUri.AbsoluteUri, username, password,
            allowInvalidServerCertificates);
        k8SClientConfig = new KubernetesClientConfiguration()
        {
            Host = cluster, SkipTlsVerify = allowInvalidServerCertificates, AccessToken = accessToken
        };
        return new Kubernetes(k8SClientConfig);
    }

    /// <summary>
    /// Builds an <see cref="HttpClient"/> for the OpenShift OAuth discovery and authorization calls.
    /// </summary>
    private static HttpClient CreateHttpClient(bool allowInvalidServerCertificates, bool allowAutoRedirect = true)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowAutoRedirect
        };
        if (allowInvalidServerCertificates)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        return new HttpClient(handler);
    }

    /// <summary>
    /// Extracts the implicit-flow access token from the OpenShift authorization redirect response.
    /// </summary>
    private static string ExtractAccessTokenFromAuthorizationResponse(HttpResponseMessage authorizationResponse)
    {
        if (!IsRedirectStatusCode(authorizationResponse.StatusCode))
        {
            var responseContent = ReadResponseContent(authorizationResponse);
            throw new InvalidOperationException(
                $"OpenShift authorization request failed with status code {(int)authorizationResponse.StatusCode}: {responseContent}");
        }

        var redirectLocation = authorizationResponse.Headers.Location ?? authorizationResponse.RequestMessage?.RequestUri;
        if (redirectLocation is null)
        {
            throw new InvalidOperationException(
                "OpenShift authorization response did not include a redirect location containing an access token.");
        }

        if (!redirectLocation.IsAbsoluteUri && authorizationResponse.RequestMessage?.RequestUri is { } requestUri)
        {
            redirectLocation = new Uri(requestUri, redirectLocation);
        }

        if (TryReadUriParameter(redirectLocation, "access_token", out var accessToken))
        {
            return accessToken;
        }

        if (TryReadUriParameter(redirectLocation, "error", out var error))
        {
            var description = TryReadUriParameter(redirectLocation, "error_description", out var errorDescription)
                ? $": {errorDescription}"
                : string.Empty;
            throw new InvalidOperationException($"OpenShift authorization failed with error '{error}'{description}");
        }

        throw new InvalidOperationException(
            $"OpenShift authorization redirect did not contain an access token. Redirect location: {redirectLocation}");
    }

    /// <summary>
    /// Reads the response body and throws a descriptive exception when the HTTP operation does not succeed.
    /// </summary>
    private static string ReadSuccessfulResponseContent(HttpResponseMessage responseMessage, string operationDescription)
    {
        var responseContent = ReadResponseContent(responseMessage);
        if (!responseMessage.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{operationDescription} failed with status code {(int)responseMessage.StatusCode}: {responseContent}");
        }

        return responseContent;
    }

    private static string ReadResponseContent(HttpResponseMessage responseMessage)
        => responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
        => (int)statusCode is >= 300 and < 400;

    /// <summary>
    /// Attempts to read a parameter from either the query string or fragment of a redirect URI.
    /// </summary>
    private static bool TryReadUriParameter(Uri uri, string parameterName, out string value)
        => TryReadFormEncodedParameter(uri.Fragment, parameterName, out value)
           || TryReadFormEncodedParameter(uri.Query, parameterName, out value);

    /// <summary>
    /// Attempts to read a single form-url-encoded parameter from a query-string or fragment payload.
    /// </summary>
    private static bool TryReadFormEncodedParameter(string valueSource, string parameterName, out string value)
    {
        var trimmedValueSource = valueSource.TrimStart('#', '?');
        foreach (var encodedPair in trimmedValueSource.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = encodedPair.Split('=', 2);
            if (!string.Equals(Uri.UnescapeDataString(pair[0]), parameterName, StringComparison.Ordinal))
            {
                continue;
            }

            value = pair.Length == 2
                ? Uri.UnescapeDataString(pair[1].Replace('+', ' '))
                : string.Empty;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
