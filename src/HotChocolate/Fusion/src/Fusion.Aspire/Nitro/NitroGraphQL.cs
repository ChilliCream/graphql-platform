using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Sends the GraphQL requests of the Nitro integration. Requests are bounded in time and in size,
/// transient transport failures are retried, and subscriptions are consumed over server-sent
/// events until the caller recognizes a terminal result.
/// </summary>
internal static class NitroGraphQL
{
    private const int MaxAttempts = 3;
    private const int MaxResponseBytes = 2 * 1024 * 1024;
    private const int MaxSubscriptionEvents = 1000;
    private static readonly TimeSpan s_requestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The time a single request may take when the caller has no bound of its own.
    /// </summary>
    public static TimeSpan DefaultRequestTimeout => s_requestTimeout;

    /// <summary>
    /// Sends a request and reads its single result.
    /// </summary>
    /// <param name="client">
    /// The GraphQL client that sends the request.
    /// </param>
    /// <param name="request">
    /// The request to send.
    /// </param>
    /// <param name="retryTransientFailures">
    /// Specifies whether a transient transport failure is retried. Only requests that are safe to
    /// send more than once can be retried.
    /// </param>
    /// <param name="requestTimeout">
    /// The time a single attempt may take. Use <see cref="Timeout.InfiniteTimeSpan"/> when the
    /// request is bounded by <paramref name="cancellationToken"/> alone.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static async Task<NitroGraphQLResult> SendWithRetryAsync(
        GraphQLHttpClient client,
        GraphQLHttpRequest request,
        bool retryTransientFailures,
        TimeSpan requestTimeout,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var attemptTimeout =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptTimeout.CancelAfter(requestTimeout);

                using var response = await client.SendAsync(request, attemptTimeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (retryTransientFailures
                        && IsTransient(response.StatusCode)
                        && attempt < MaxAttempts)
                    {
                        await DelayRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    return NitroGraphQLResult.Failed(
                        $"Nitro returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                }

                if (response.ContentHeaders.ContentLength is > MaxResponseBytes)
                {
                    return NitroGraphQLResult.Failed(
                        "Nitro returned a GraphQL response that exceeded the size limit.");
                }

                using var document = await ReadBoundedJsonAsync(
                    response.HttpResponseMessage,
                    attemptTimeout.Token);
                var root = document.RootElement;
                if (root.TryGetProperty("errors", out var errors)
                    && errors.ValueKind is JsonValueKind.Array
                    && errors.GetArrayLength() > 0)
                {
                    return NitroGraphQLResult.Failed(DescribeErrors(errors));
                }

                if (!root.TryGetProperty("data", out var data))
                {
                    return NitroGraphQLResult.Failed("Nitro returned a malformed GraphQL response.");
                }

                return NitroGraphQLResult.Success(data.Clone());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (
                retryTransientFailures && attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
            }
            catch (HttpRequestException) when (
                retryTransientFailures && attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
            }
            catch (IOException) when (
                retryTransientFailures && attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
            }
            catch (NitroResponseTooLargeException exception)
            {
                return NitroGraphQLResult.Failed(exception.Message);
            }
        }
    }

    /// <summary>
    /// Subscribes to a Nitro subscription and waits for the terminal result of the operation. The
    /// subscription is consumed over server-sent events and every event is handed to
    /// <paramref name="readTerminalResult"/>, which returns <c>null</c> for an event that the
    /// operation has not completed with.
    /// </summary>
    /// <param name="client">
    /// The GraphQL client that sends the subscription.
    /// </param>
    /// <param name="request">
    /// The subscription request to send.
    /// </param>
    /// <param name="readTerminalResult">
    /// Reads the terminal result of the operation from an event, or returns <c>null</c> when the
    /// operation is still running. The values of an event are only valid for the duration of the
    /// call, so everything that outlives the call has to be copied.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token. It bounds the subscription as a whole.
    /// </param>
    /// <returns>
    /// The terminal result of the operation, or the reason why none was received. A subscription
    /// that ends without a terminal result is a failure.
    /// </returns>
    public static async Task<(T? Value, string? Failure)> SubscribeAsync<T>(
        GraphQLHttpClient client,
        GraphQLHttpRequest request,
        Func<OperationResult, T?> readTerminalResult,
        CancellationToken cancellationToken)
        where T : class
    {
        var (response, failure) = await ConnectAsync(client, request, cancellationToken);

        if (response is null)
        {
            return (null, failure);
        }

        using (response)
        {
            // Once the first event can arrive the subscription is no longer safe to send again,
            // so the stream itself is never retried.
            return await ReadSubscriptionAsync(response, readTerminalResult, cancellationToken);
        }
    }

    /// <summary>
    /// Subscribes to a Nitro subscription and reads every event of the subscription until it
    /// ends. The subscription is consumed over server-sent events.
    /// </summary>
    /// <param name="client">
    /// The GraphQL client that sends the subscription.
    /// </param>
    /// <param name="request">
    /// The subscription request to send.
    /// </param>
    /// <param name="readEvent">
    /// Reads an event of the subscription. The values of an event are only valid for the duration
    /// of the call, so everything that outlives the call has to be copied.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token. It bounds the subscription as a whole.
    /// </param>
    /// <exception cref="NitroSubscriptionException">
    /// The subscription could not be established, or Nitro ended it with an error.
    /// </exception>
    public static async IAsyncEnumerable<T> StreamAsync<T>(
        GraphQLHttpClient client,
        GraphQLHttpRequest request,
        Func<OperationResult, T> readEvent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (response, failure) = await ConnectAsync(client, request, cancellationToken);

        if (response is null)
        {
            throw new NitroSubscriptionException(failure!);
        }

        using (response)
        {
            var eventCount = 0;

            await foreach (var result in response.ReadAsResultStreamAsync()
                .WithCancellation(cancellationToken))
            {
                using (result)
                {
                    if (++eventCount > MaxSubscriptionEvents)
                    {
                        throw new NitroSubscriptionException(
                            "Nitro sent too many subscription events.");
                    }

                    if (result.Errors.ValueKind is JsonValueKind.Array
                        && result.Errors.GetArrayLength() > 0)
                    {
                        throw new NitroSubscriptionException(DescribeErrors(result.Errors));
                    }

                    yield return readEvent(result);
                }
            }
        }
    }

    /// <summary>
    /// Establishes a subscription over server-sent events. The returned response is owned by the
    /// caller and no event has been read from it yet.
    /// </summary>
    private static async Task<(GraphQLHttpResponse? Response, string? Failure)> ConnectAsync(
        GraphQLHttpClient client,
        GraphQLHttpRequest request,
        CancellationToken cancellationToken)
    {
        request.Accept = GraphQLHttpRequest.GraphQLOverSse;

        for (var attempt = 1; ; attempt++)
        {
            GraphQLHttpResponse response;

            try
            {
                response = await SubscribeCoreAsync(client, request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
                continue;
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
                continue;
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                await DelayRetryAsync(attempt, cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                response.Dispose();

                if (IsTransient(statusCode) && attempt < MaxAttempts)
                {
                    await DelayRetryAsync(attempt, cancellationToken);
                    continue;
                }

                return (null, $"Nitro returned HTTP {(int)statusCode} ({statusCode}).");
            }

            // A server that ignores the pinned Accept header answers with a single result
            // instead of a stream, which would end the subscription after one event. That is
            // a protocol failure and not the end of a subscription.
            var mediaType = response.ContentHeaders.ContentType?.MediaType;
            if (!string.Equals(mediaType, ContentType.EventStream, StringComparison.OrdinalIgnoreCase))
            {
                response.Dispose();

                return (
                    null,
                    "Nitro answered the subscription with the content type "
                        + $"'{mediaType ?? "none"}' instead of {ContentType.EventStream}.");
            }

            return (response, null);
        }
    }

    private static async Task<GraphQLHttpResponse> SubscribeCoreAsync(
        GraphQLHttpClient client,
        GraphQLHttpRequest request,
        CancellationToken cancellationToken)
    {
        // The request timeout only covers the subscribe call, which completes once the response
        // headers have been read. The stream that follows is bounded by the cancellation token of
        // the caller, so the timeout is released before the first event is read.
        using var requestTimeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestTimeout.CancelAfter(s_requestTimeout);

        return await client.SendAsync(request, requestTimeout.Token);
    }

    private static async Task<(T? Value, string? Failure)> ReadSubscriptionAsync<T>(
        GraphQLHttpResponse response,
        Func<OperationResult, T?> readTerminalResult,
        CancellationToken cancellationToken)
        where T : class
    {
        var eventCount = 0;

        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(cancellationToken))
        {
            using (result)
            {
                if (++eventCount > MaxSubscriptionEvents)
                {
                    return (null, "Nitro sent too many subscription events.");
                }

                if (result.Errors.ValueKind is JsonValueKind.Array
                    && result.Errors.GetArrayLength() > 0)
                {
                    return (null, DescribeErrors(result.Errors));
                }

                var value = readTerminalResult(result);
                if (value is not null)
                {
                    return (value, null);
                }
            }
        }

        return (null, "Nitro ended the subscription without a result.");
    }

    private static string DescribeErrors(JsonElement errors)
    {
        var error = errors[0];

        return error.ValueKind is JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind is JsonValueKind.String
                ? $"Nitro returned GraphQL errors: {message.GetString()}"
                : "Nitro returned GraphQL errors.";
    }

    private static async Task<JsonDocument> ReadBoundedJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (destination.Length + read > MaxResponseBytes)
            {
                throw new NitroResponseTooLargeException(
                    "Nitro returned a GraphQL response that exceeded the size limit.");
            }

            destination.Write(buffer, 0, read);
        }

        destination.Position = 0;
        return await JsonDocument.ParseAsync(destination, default, cancellationToken);
    }

    private static async Task DelayRetryAsync(int attempt, CancellationToken cancellationToken)
        => await Task.Delay(
            TimeSpan.FromMilliseconds(250 * (1 << (attempt - 1))),
            cancellationToken);

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            || statusCode >= HttpStatusCode.InternalServerError;

    private sealed class NitroResponseTooLargeException(string message) : Exception(message);
}

/// <summary>
/// A Nitro subscription could not be established, or Nitro ended it with an error.
/// </summary>
internal sealed class NitroSubscriptionException(string message) : Exception(message);
