using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.InteropServices;
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
    private readonly ConcurrentDictionary<RequestKey, Lazy<TaskCompletionSource<BufferedHttpResponse>>>
        _inFlight = new();
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
            key = default;
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

    private async ValueTask<RequestKey> ComputeRequestKeyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Body
        byte[] body = [];

        if (request.Content is not null)
        {
            body = await request.Content
                .ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        // Configured headers (pre-sorted for determinism)
        string[]?[] headerValues = [];

        for (var i = 0; i < _hashHeaders.Length; i++)
        {
            if (request.Headers.TryGetValues(_hashHeaders[i], out var values))
            {
                if (headerValues.Length == 0)
                {
                    headerValues = new string[]?[_hashHeaders.Length];
                }

                headerValues[i] = values as string[] ?? [.. values];
            }
        }

        return new RequestKey(
            request.Method.Method,
            request.RequestUri?.AbsoluteUri,
            headerValues,
            body);
    }

    private readonly struct RequestKey : IEquatable<RequestKey>
    {
        private static readonly long s_seed = Random.Shared.NextInt64();

        private readonly string _method;
        private readonly string? _uri;
        private readonly string[]?[] _headerValues;
        private readonly byte[] _body;
        private readonly int _hashCode;

        public RequestKey(string method, string? uri, string[]?[] headerValues, byte[] body)
        {
            _method = method;
            _uri = uri;
            _headerValues = headerValues;
            _body = body;

            var hashCode = new HashCode();
            hashCode.Add(method);
            hashCode.Add(uri);

            foreach (var values in headerValues)
            {
                foreach (var value in values ?? [])
                {
                    var valueBytes = MemoryMarshal.AsBytes(value.AsSpan());
                    hashCode.Add(XxHash3.HashToUInt64(valueBytes, s_seed));
                }
            }

            hashCode.Add(XxHash3.HashToUInt64(body, s_seed));
            _hashCode = hashCode.ToHashCode();
        }

        public bool Equals(RequestKey other)
        {
            if (!string.Equals(_method, other._method, StringComparison.Ordinal)
                || !string.Equals(_uri, other._uri, StringComparison.Ordinal)
                || _headerValues.Length != other._headerValues.Length)
            {
                return false;
            }

            for (var i = 0; i < _headerValues.Length; i++)
            {
                ReadOnlySpan<string> values = _headerValues[i] ?? [];

                if (!values.SequenceEqual(other._headerValues[i] ?? []))
                {
                    return false;
                }
            }

            return _body.AsSpan().SequenceEqual(other._body);
        }

        public override bool Equals(object? obj)
            => obj is RequestKey other && Equals(other);

        public override int GetHashCode()
            => _hashCode;
    }
}
