using System.Text.Json;

namespace eShop.Gateway.BenchmarkDiagnostics;

internal sealed class WireCaptureHandler(string path) : DelegatingHandler
{
    private static readonly object s_sync = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var row = JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            url = request.RequestUri,
            authorization = request.Headers.Authorization?.ToString(),
            body
        });
        lock (s_sync)
        {
            File.AppendAllText(path, row + Environment.NewLine);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
