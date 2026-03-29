using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Text;
using HotChocolate.Language;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// A <see cref="DelegatingHandler"/> that deduplicates identical in-flight HTTP requests.
/// When an identical request is already in progress, subsequent callers wait for the
/// leader's response instead of sending a duplicate request over the network.
/// <para>
/// Only GraphQL query operations are deduplicated. Mutations, subscriptions,
/// and requests without an operation kind hint pass through unchanged.
/// The operation kind is signaled via <see cref="HttpRequestMessage.Options"/>
/// using the <see cref="GraphQLHttpRequest.OperationKindOptionsKey"/>.
/// </para>
/// </summary>
public sealed class RequestDeduplicationHandler : DelegatingHandler
{
    private readonly ConcurrentDictionary<ulong, Lazy<TaskCompletionSource<BufferedHttpResponse>>> _inFlight = new();
    private readonly ImmutableArray<string> _hashHeaders;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestDeduplicationHandler"/>.
    /// </summary>
    /// <param name="options">The deduplication options.</param>
    public RequestDeduplicationHandler(RequestDeduplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Sort header names once at construction time for deterministic hashing.
        _hashHeaders = [.. options.HashHeaders.Sort(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RequestDeduplicationHandler"/>
    /// with default options.
    /// </summary>
    public RequestDeduplicationHandler()
        : this(new RequestDeduplicationOptions())
    {
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!CanDeduplicateRequest(request))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // We compute the dedup key from method + URI + body + configured headers.
        var key = await ComputeRequestKeyAsync(request, cancellationToken).ConfigureAwait(false);

        // use a Lazy<TCS> so that under burst conditions only one TCS is materialized even
        // if multiple requests race through GetOrAdd concurrently.
        var entry = new Lazy<TaskCompletionSource<BufferedHttpResponse>>(
            static () => new TaskCompletionSource<BufferedHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously));

        var existing = _inFlight.GetOrAdd(key, entry);

        if (!ReferenceEquals(existing, entry))
        {
            // Follower path: another request is already in flight.
            var result = await existing.Value.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return result.CreateResponse();
        }

        // Leader path: execute the real request.
        try
        {
            using var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // We capture the original response into a buffered snapshot that can be cloned for each follower.
            var deduped = await BufferedHttpResponse
                .CaptureAsync(response, cancellationToken)
                .ConfigureAwait(false);
            entry.Value.TrySetResult(deduped);
            return deduped.CreateResponse();
        }
        catch (OperationCanceledException ex)
        {
            entry.Value.TrySetCanceled(ex.CancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            entry.Value.TrySetException(ex);
            throw;
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    private static bool CanDeduplicateRequest(HttpRequestMessage request)
    {
        // Skip multipart/file-upload requests.
        if (request.Content is MultipartFormDataContent)
        {
            return false;
        }

        // Only deduplicate if the operation kind is explicitly set to Query.
        if (!request.Options.TryGetValue(GraphQLHttpRequest.OperationKindOptionsKey, out var operationType))
        {
            return false;
        }

        return operationType is OperationType.Query;
    }

    private async ValueTask<ulong> ComputeRequestKeyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var hash = new XxHash64();

        // Method
        AppendString(ref hash, request.Method.Method);

        // URI
        if (request.RequestUri is not null)
        {
            AppendString(ref hash, request.RequestUri.AbsoluteUri);
        }

        // Body
        if (request.Content is not null)
        {
            var bodyBytes = await request.Content
                .ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            hash.Append(bodyBytes);
        }

        // Configured headers (pre-sorted for determinism)
        foreach (var headerName in _hashHeaders)
        {
            if (request.Headers.TryGetValues(headerName, out var values))
            {
                AppendString(ref hash, headerName);

                foreach (var value in values)
                {
                    AppendString(ref hash, value);
                }
            }
        }

        return hash.GetCurrentHashAsUInt64();
    }

    private static void AppendString(ref XxHash64 hash, string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);

        if (maxByteCount <= 256)
        {
            Span<byte> buffer = stackalloc byte[maxByteCount];
            var bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
            hash.Append(buffer[..bytesWritten]);
        }
        else
        {
            hash.Append(Encoding.UTF8.GetBytes(value));
        }
    }
}
