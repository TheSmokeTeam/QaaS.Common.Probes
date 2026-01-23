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
    private static readonly HttpClientHandler HttpClientHandler = new()
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };

    private static readonly HttpClient HttpClient = new(HttpClientHandler);

    /// <summary>
    /// Get the authorization and token endpoints of the Openshift cluster.
    /// Both endpoints lead to the cluster's required url through which the access token of a user is generated.
    /// </summary>
    /// <param name="host">The url of the openshift cluster</param>
    /// <returns>Tuple containing both auth url and token url.</returns>
    private static (string, string) DiscoverAuthUrl(string host)
    {
        if (host[^1] == '/')
            host = host.Remove(host.Length - 1);

        var url = $"{host}/.well-known/oauth-authorization-server";
        var response = HttpClient.GetAsync(url).Result;
        var oauthInfo = response.Content.ReadAsStringAsync().Result;
        var jsonAuthInfo = JObject.Load(new JsonTextReader(new StringReader(oauthInfo)));
        return (jsonAuthInfo["authorization_endpoint"]!.ToString(), jsonAuthInfo["token_endpoint"]!.ToString());
    }

    private static string ExtractToken(HttpResponseMessage responseMessage)
    {
        var jsonAuthInfo =
            JObject.Load(new JsonTextReader(new StringReader(responseMessage.Content.ReadAsStringAsync().Result)));

        return jsonAuthInfo["access_token"]!.ToString();
    }

    /// <summary>
    /// Get the access token of the given username and password to the given Openshift cluster.
    /// </summary>
    /// <param name="host">The url of the openshift cluster</param>
    /// <param name="username">The username to authenticate.</param>
    /// <param name="password">The password of the username to authenticate.</param>
    /// <returns>An access token to the Openshift cluster.</returns>
    private static string GetToken(string host, string username, string password)
    {
        var (authorizationEndpoint, tokenEndpoint) = DiscoverAuthUrl(host);

        var authUrl =
            $"{authorizationEndpoint}?response_type=code&client_id=openshift-challenging-client&state=1&code_challenge_method=S256";

        var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");

        // Request to get the sha256 code to send to the token endpoint
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, authUrl);
        httpRequest.Headers.Add("authorization", $"Basic {Convert.ToBase64String(authBytes)}");
        httpRequest.Headers.Add("X-Csrf-Token", "1");
        var response = HttpClient.SendAsync(httpRequest).Result;

        var location = response.RequestMessage!.RequestUri!.AbsoluteUri;
        var sha256Code = location.Substring(location.IndexOf("?", StringComparison.Ordinal) + 1);

        // Request to send the sha256 code from the previous GET request to the token endpoint
        httpRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        httpRequest.Headers.Add("Accept", "application/json");
        httpRequest.Headers.Add("Authorization", "Basic REDA=");
        httpRequest.Content = new StringContent($"{sha256Code}&grant_type=authorization_code", Encoding.UTF8,
            "application/x-www-form-urlencoded");
        response = HttpClient.SendAsync(httpRequest).Result;

        return ExtractToken(response);
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
    /// <returns>The changed <see cref="Kubernetes"/> object.</returns>
    public static Kubernetes CreateKubernetesClient(string cluster, string username, string password)
    {
        if (!cluster.StartsWith("http"))
            cluster = $"https://{cluster}";

        var k8SClientConfig = new KubernetesClientConfiguration
            { Host = cluster, SkipTlsVerify = true };
        var k8SClientWithNoToken = new Kubernetes(k8SClientConfig);
        var accessToken = GetToken(k8SClientWithNoToken.BaseUri.AbsoluteUri, username, password);
        k8SClientConfig = new KubernetesClientConfiguration()
        {
            Host = cluster, SkipTlsVerify = true, AccessToken = accessToken
        };
        return new Kubernetes(k8SClientConfig);
    }
}