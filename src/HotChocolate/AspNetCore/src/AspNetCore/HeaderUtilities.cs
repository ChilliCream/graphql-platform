using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Utilities for handling HTTP headers.
/// </summary>
internal static class HeaderUtilities
{
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache =
        new(StringComparer.Ordinal);

    public static readonly AcceptMediaType[] GraphQLResponseContentTypes =
    [
        new AcceptMediaType(
            ContentType.Types.Application,
            ContentType.SubTypes.GraphQLResponse,
            null,
            StringSegment.Empty),
    ];

    /// <summary>
    /// Gets the parsed accept header values from a request.
    /// </summary>
    /// <param name="request">
    /// The HTTP request.
    /// </param>
    public static AcceptHeaderResult GetAcceptHeader(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderNames.Accept, out var value))
        {
            var count = value.Count;

            if (count == 0)
            {
                return new AcceptHeaderResult(Array.Empty<AcceptMediaType>());
            }

            string[] innerArray;

            if (count == 1)
            {
                var headerValue = value[0]!;

                if (TryParseMediaType(headerValue, out var parsedValue))
                {
                    return new AcceptHeaderResult([parsedValue,]);
                }

                // note: this is a workaround for now. we need to parse this properly.
                if (headerValue.IndexOf(',', StringComparison.Ordinal) != -1)
                {
                    innerArray = headerValue.Split(',');
                    goto MULTI_VALUES;
                }

                return new AcceptHeaderResult(headerValue);
            }

            innerArray = value!;

MULTI_VALUES:
            ref var searchSpace = ref MemoryMarshal.GetReference(innerArray.AsSpan());
            var parsedValues = new AcceptMediaType[innerArray.Length];
            var p = 0;

            for (var i = 0; i < innerArray.Length; i++)
            {
                var mediaType = Unsafe.Add(ref searchSpace, i);
                if (TryParseMediaType(mediaType, out var parsedValue))
                {
                    parsedValues[p++] = parsedValue;
                }
                else
                {
                    return new AcceptHeaderResult(mediaType);
                }
            }

            if (parsedValues.Length > p)
            {
                Array.Resize(ref parsedValues, p);
            }

            return new AcceptHeaderResult(parsedValues);
        }

        return new AcceptHeaderResult(Array.Empty<AcceptMediaType>());
    }

    private static bool TryParseMediaType(string s, out AcceptMediaType value)
    {
        MakeSpace();

        // first we try to look up the parsed header in the cache.
        // if we find it the string was a valid header value and
        // we return it.
        if (_cache.TryGetValue(s, out var entry))
        {
            value = entry.Value;
            return true;
        }

        // if not we will try to parse it.
        if (MediaTypeHeaderValue.TryParse(s, out var parsedValue))
        {
            entry = _cache.GetOrAdd(s, k => new CacheEntry(k, parsedValue));
            value = entry.Value;
            return true;
        }

        value = default;
        return false;
    }

    private static void MakeSpace()
    {
        // if we reach the maximum available space we will remove around 20% of the cached items.
        if (_cache.Count > 100)
        {
            foreach (var entry in _cache.Values.OrderBy(t => t.CreatedAt).Take(20))
            {
                _cache.TryRemove(entry.Key, out _);
            }
        }
    }

    private readonly struct CacheEntry
    {
        public CacheEntry(string key, MediaTypeHeaderValue value)
        {
            Key = key;
            Value = new AcceptMediaType(value.Type, value.SubType, value.Quality, value.Charset);
            CreatedAt = DateTime.UtcNow;
        }

        public string Key { get; }

        public AcceptMediaType Value { get; }

        public DateTime CreatedAt { get; }
    }

    internal readonly struct AcceptHeaderResult
    {
        public AcceptHeaderResult(AcceptMediaType[] acceptMediaTypes)
        {
            AcceptMediaTypes = acceptMediaTypes;
            ErrorResult = null;
            HasError = false;
        }

        public AcceptHeaderResult(string headerValue)
        {
            AcceptMediaTypes = [];
            ErrorResult = ErrorHelper.InvalidAcceptMediaType(headerValue);
            HasError = true;
        }

        public AcceptMediaType[] AcceptMediaTypes { get; }

        public IOperationResult? ErrorResult { get; }

        [MemberNotNullWhen(true, nameof(ErrorResult))]
        public bool HasError { get; }
    }
}
