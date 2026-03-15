using System.Net;
using System.Text;
using k8s;
using NUnit.Framework;

namespace QaaS.Common.Probes.Tests;

internal sealed class TestHttpServer : IDisposable
{
    private readonly Queue<Func<HttpListenerContext, Task>> _handlers = new();
    private readonly HttpListener _listener = new();
    private readonly Task _listenerTask;

    public TestHttpServer()
    {
        BaseAddress = CreateBaseAddress();
        _listener.Prefixes.Add(BaseAddress);
        _listener.Start();
        _listenerTask = Task.Run(ListenAsync);
    }

    public string BaseAddress { get; }

    public Kubernetes CreateKubernetesClient()
    {
        return new Kubernetes(new KubernetesClientConfiguration
        {
            Host = BaseAddress,
            SkipTlsVerify = true
        });
    }

    public void EnqueueJsonResponse(string method, string absolutePath, string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK, Action<HttpListenerRequest>? assertRequest = null,
        Action<string>? assertBody = null)
    {
        Enqueue(async context =>
        {
            Assert.That(context.Request.HttpMethod, Is.EqualTo(method));
            Assert.That(context.Request.Url, Is.Not.Null);
            Assert.That(context.Request.Url!.AbsolutePath, Is.EqualTo(absolutePath));
            assertRequest?.Invoke(context.Request);

            if (assertBody != null)
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                assertBody(await reader.ReadToEndAsync());
            }

            await WriteResponseAsync(context.Response, responseBody, statusCode);
        });
    }

    public void Enqueue(Func<HttpListenerContext, Task> handler)
    {
        _handlers.Enqueue(handler);
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();

        try
        {
            _listenerTask.GetAwaiter().GetResult();
        }
        catch (HttpListenerException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static string CreateBaseAddress()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var port = Random.Shared.Next(20000, 60000);
            var address = $"http://127.0.0.1:{port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(address);

            try
            {
                listener.Start();
                listener.Stop();
                return address;
            }
            catch (HttpListenerException)
            {
            }
        }

        throw new InvalidOperationException("Failed to bind an HTTP listener to a free local port.");
    }

    private async Task ListenAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (_handlers.Count <= 0)
            {
                await WriteResponseAsync(context.Response, "{\"error\":\"unexpected request\"}",
                    HttpStatusCode.InternalServerError);
                continue;
            }

            var handler = _handlers.Dequeue();
            await handler(context);
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string responseBody,
        HttpStatusCode statusCode)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";

        var bytes = Encoding.UTF8.GetBytes(responseBody);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }
}
