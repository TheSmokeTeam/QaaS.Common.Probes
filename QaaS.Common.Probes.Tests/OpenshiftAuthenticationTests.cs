using System.Net;
using System.Text;
using NUnit.Framework;
using QaaS.Common.Probes.Extensions;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OpenshiftAuthenticationTests
{
    [Test]
    public void CreateKubernetesClient_WhenOauthEndpointsRespond_ReturnsAuthenticatedClient()
    {
        using var listener = CreateStartedListener(out var port);

        var serverTask = Task.Run(async () =>
        {
            await HandleOauthDiscoveryAsync(listener, port);
            await HandleAuthorizationAsync(listener, port);
        });

        var kubernetesClient = OpenshiftAuthentication.CreateKubernetesClient(
            $"http://127.0.0.1:{port}",
            "user",
            "pass");

        serverTask.GetAwaiter().GetResult();

        Assert.That(kubernetesClient.BaseUri.AbsoluteUri, Is.EqualTo($"http://127.0.0.1:{port}/"));
    }

    [Test]
    public void CreateKubernetesClient_WhenOauthDiscoveryFails_ShouldThrowHelpfulException()
    {
        using var listener = CreateStartedListener(out _);

        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("discovery failed"));
            context.Response.Close();
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenshiftAuthentication.CreateKubernetesClient(listener.Prefixes.Single().TrimEnd('/'), "user", "pass"));

        serverTask.GetAwaiter().GetResult();

        Assert.That(exception!.Message, Does.Contain("OpenShift OAuth discovery request"));
        Assert.That(exception.Message, Does.Contain("500"));
    }

    [Test]
    public void CreateKubernetesClient_WhenAuthorizationFails_ShouldThrowHelpfulException()
    {
        using var listener = CreateStartedListener(out var port);

        var serverTask = Task.Run(async () =>
        {
            await HandleOauthDiscoveryAsync(listener, port);

            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 401;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("unauthorized"));
            context.Response.Close();
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenshiftAuthentication.CreateKubernetesClient($"http://127.0.0.1:{port}", "user", "pass"));

        serverTask.GetAwaiter().GetResult();

        Assert.That(exception!.Message, Does.Contain("OpenShift authorization request failed"));
        Assert.That(exception.Message, Does.Contain("401"));
    }

    [Test]
    public void CreateKubernetesClient_WhenAuthorizationRedirectDoesNotContainAccessToken_ShouldThrowHelpfulException()
    {
        using var listener = CreateStartedListener(out var port);

        var serverTask = Task.Run(async () =>
        {
            await HandleOauthDiscoveryAsync(listener, port);

            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = $"http://127.0.0.1:{port}/callback?code=test-code&state=1";
            context.Response.Close();
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenshiftAuthentication.CreateKubernetesClient($"http://127.0.0.1:{port}", "user", "pass"));

        serverTask.GetAwaiter().GetResult();

        Assert.That(exception, Is.Not.TypeOf<NullReferenceException>());
        Assert.That(exception!.Message, Does.Contain("did not contain an access token"));
    }

    private static async Task HandleOauthDiscoveryAsync(HttpListener listener, int port)
    {
        var context = await listener.GetContextAsync();
        context.Response.StatusCode = 200;
        var payload =
            $$"""
              {"authorization_endpoint":"http://127.0.0.1:{{port}}/auth","token_endpoint":"http://127.0.0.1:{{port}}/token"}
              """;
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(payload));
        context.Response.Close();
    }

    private static async Task HandleAuthorizationAsync(HttpListener listener, int port)
    {
        var context = await listener.GetContextAsync();
        context.Response.StatusCode = 302;
        context.Response.RedirectLocation =
            $"http://127.0.0.1:{port}/callback#access_token=token-value&token_type=Bearer&state=1";
        context.Response.Close();
        await Task.CompletedTask;
    }

    private static HttpListener CreateStartedListener(out int port)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            port = Random.Shared.Next(20000, 60000);
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");

            try
            {
                listener.Start();
                return listener;
            }
            catch (HttpListenerException)
            {
                listener.Close();
            }
        }

        throw new InvalidOperationException("Failed to bind an HTTP listener to a free local port.");
    }
}
