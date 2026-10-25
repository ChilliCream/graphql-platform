using System.Net;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Client;

/// <summary>
/// Answers every request with one canned GraphQL response and records the variables that were sent, so the
/// generated Nitro API client runs unmodified.
/// </summary>
internal sealed class FakeNitroApi(string responseJson) : HttpMessageHandler, IHttpClientFactory
{
    public JsonElement Variables { get; private set; }

    public TelemetryClient CreateTelemetryClient()
        => new(NitroClientServiceCollectionExtensions.AddNitroApiClient(this));

    public HttpClient CreateClient(string name)
        => new(this, disposeHandler: false) { BaseAddress = new Uri("http://localhost/graphql") };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        Variables = document.RootElement.GetProperty("variables").Clone();

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/graphql-response+json")
        };
    }
}
