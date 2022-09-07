using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore;

internal static class HeaderUtilities
{
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache =
        new(StringComparer.Ordinal);

    public static AcceptMediaType[] GetAcceptHeader(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderNames.Accept, out var value))
        {
            var count = value.Count;

            if (count == 0)
            {
                return Array.Empty<AcceptMediaType>();
            }

            if (count == 1)
            {
                if (TryParseMediaType(value[0], out var parsedValue))
                {
                    return new[] { parsedValue };
                }

                return Array.Empty<AcceptMediaType>();
            }

            string[] innerArray = value;
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
            }

            if (parsedValues.Length > p)
            {
                Array.Resize(ref parsedValues, p);
            }

            return parsedValues;
        }

        return Array.Empty<AcceptMediaType>();
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
}
