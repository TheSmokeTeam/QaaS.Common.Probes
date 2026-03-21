using System.Net.Http.Headers;

namespace QaaS.Common.Probes.Tests;

internal sealed class HttpRecordingMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    public List<CapturedHttpRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new CapturedHttpRequest(
            request.Method,
            request.RequestUri?.ToString(),
            body,
            request.Headers.Authorization));

        return responder(request);
    }
}

internal sealed record CapturedHttpRequest(HttpMethod Method, string? RequestUri, string? Body,
    AuthenticationHeaderValue? Authorization);
