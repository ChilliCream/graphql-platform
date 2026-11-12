using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion;

/// <summary>
/// Reads the server-sent events of a GraphQL subscription over a raw HTTP response.
/// </summary>
public sealed class SubscriptionSseClient : IDisposable
{
    private readonly HttpResponseMessage _response;
    private readonly StreamReader _reader;

    private SubscriptionSseClient(HttpResponseMessage response, StreamReader reader)
    {
        _response = response;
        _reader = reader;
    }

    public HttpStatusCode StatusCode => _response.StatusCode;

    public string? Challenge
        => _response.Headers.TryGetValues("WWW-Authenticate", out var values) ? string.Join(", ", values) : null;

    public static async Task<SubscriptionSseClient> StartAsync(
        HttpClient client,
        string query,
        string? user = null,
        string? expiry = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { query }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (user is not null)
        {
            request.Headers.Add(SubscriptionPoliciesTransportTestBase.UserHeader, user);
        }

        if (expiry is not null)
        {
            request.Headers.Add(SubscriptionPoliciesTransportTestBase.ExpiryHeader, expiry);
        }

        var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);

        return new SubscriptionSseClient(response, new StreamReader(stream, Encoding.UTF8));
    }

    /// <summary>
    /// Reads the next event as <c>event: name</c> followed by its indented data, or <c>null</c>
    /// when the response ended.
    /// </summary>
    public async Task<string?> ReadEventAsync()
    {
        string? name = null;
        string? data = null;

        while (await _reader.ReadLineAsync(TestContext.Current.CancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                if (name is null && data is null)
                {
                    continue;
                }

                return data is null ? $"event: {name}" : $"event: {name}\n{WireJson.Indent(data)}";
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                name = line["event: ".Length..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data = line["data: ".Length..];
            }
        }

        return null;
    }

    /// <summary>
    /// Reads every remaining event until the response ends.
    /// </summary>
    public async Task<ImmutableArray<string>> ReadToEndAsync()
    {
        var events = ImmutableArray.CreateBuilder<string>();

        while (await ReadEventAsync() is { } next)
        {
            events.Add(next);
        }

        return events.ToImmutable();
    }

    public void Dispose()
    {
        _reader.Dispose();
        _response.Dispose();
    }
}
