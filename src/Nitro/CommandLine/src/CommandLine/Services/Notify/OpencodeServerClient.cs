using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// HTTP client for the opencode server API used to deliver a prompt or verify
/// that a registered session remains available.
/// </summary>
internal sealed class OpencodeServerClient : IOpencodeServerClient
{
    private static readonly TimeSpan s_timeout = PingPolicy.HardTimeout;

    private readonly HttpClient _client;
    private readonly TimeSpan _timeout;

    public OpencodeServerClient(IHttpClientFactory clientFactory)
        : this(clientFactory.CreateClient(nameof(OpencodeServerClient)), s_timeout)
    {
    }

    internal OpencodeServerClient(HttpClient client, TimeSpan timeout)
    {
        _client = client;
        _timeout = timeout;
    }

    public async Task<string> PushMessageAsync(
        string serverUrl,
        string sessionId,
        string text,
        string? secret,
        CancellationToken cancellationToken)
    {
        if (!TryCreateUri(serverUrl, $"session/{Uri.EscapeDataString(sessionId)}/message", out var uri))
        {
            return AgentPingResult.EndpointGone;
        }

        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = CreateMessageContent(text)
        };

        AddAuthentication(request, secret);

        try
        {
            using var response = await _client.SendAsync(request, linkedSource.Token);
            return response.IsSuccessStatusCode ? AgentPingResult.Ok : AgentPingResult.EndpointGone;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return AgentPingResult.Timeout;
        }
        catch (HttpRequestException)
        {
            return AgentPingResult.EndpointGone;
        }
    }

    public async Task<string> PingAsync(
        string serverUrl,
        string sessionId,
        string? secret,
        CancellationToken cancellationToken)
    {
        if (!TryCreateUri(serverUrl, "global/health", out var healthUri)
            || !TryCreateUri(serverUrl, $"session/{Uri.EscapeDataString(sessionId)}", out var sessionUri))
        {
            return AgentPingResult.EndpointGone;
        }

        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            using (var healthRequest = new HttpRequestMessage(HttpMethod.Get, healthUri))
            {
                AddAuthentication(healthRequest, secret);

                using var healthResponse = await _client.SendAsync(healthRequest, linkedSource.Token);

                if (!healthResponse.IsSuccessStatusCode)
                {
                    return AgentPingResult.EndpointGone;
                }
            }

            using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, sessionUri);
            AddAuthentication(sessionRequest, secret);

            using var sessionResponse = await _client.SendAsync(sessionRequest, linkedSource.Token);
            return sessionResponse.IsSuccessStatusCode ? AgentPingResult.Ok : AgentPingResult.EndpointGone;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return AgentPingResult.Timeout;
        }
        catch (HttpRequestException)
        {
            return AgentPingResult.EndpointGone;
        }
    }

    private static void AddAuthentication(HttpRequestMessage request, string? secret)
    {
        if (secret is null)
        {
            return;
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"opencode:{secret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private static HttpContent CreateMessageContent(string text)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("parts");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("type", "text");
            writer.WriteString("text", text);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        var content = new ByteArrayContent(buffer.WrittenSpan.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static bool TryCreateUri(string serverUrl, string path, out Uri? uri)
    {
        uri = null;

        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        var separator = baseUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? string.Empty : "/";
        return Uri.TryCreate(baseUri.AbsoluteUri + separator + path, UriKind.Absolute, out uri);
    }
}
