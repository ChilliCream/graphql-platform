using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore.Utilities;

/// <summary>
/// Utilities for handling HTTP headers.
/// </summary>
internal static class HeaderUtilities
{
    private static readonly Cache<AcceptHeaderResult> s_headerCache = new(128);
    private static readonly Cache<AcceptMediaType> s_mediaTypeCache = new(128);

    public static readonly AcceptMediaType[] GraphQLResponseContentTypes =
    [
        new AcceptMediaType(
            ContentType.Types.Application,
            ContentType.SubTypes.GraphQLResponse,
            null,
            StringSegment.Empty)
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
                return new AcceptHeaderResult([]);
            }

            if (count == 1)
            {
                var headerValue = value[0]!;

                if (s_headerCache.TryGet(headerValue, out var cached))
                {
                    return cached;
                }

                var result = ParseHeaderValue(headerValue);

                if (!result.HasError)
                {
                    s_headerCache.TryAdd(headerValue, result);
                }

                return result;
            }

            return ParseMultipleHeaderValues(value!);
        }

        return new AcceptHeaderResult([]);
    }

    private static AcceptHeaderResult ParseHeaderValue(string headerValue)
    {
        if (TryParseMediaType(headerValue, out var parsedValue))
        {
            return new AcceptHeaderResult([parsedValue]);
        }

        // note: this is a workaround for now. we need to parse this properly.
        if (headerValue.IndexOf(',', StringComparison.Ordinal) != -1)
        {
            return ParseMultipleHeaderValues(headerValue.Split(','));
        }

        return new AcceptHeaderResult(headerValue);
    }

    private static AcceptHeaderResult ParseMultipleHeaderValues(string[] innerArray)
    {
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

    private static bool TryParseMediaType(string s, out AcceptMediaType value)
    {
        if (s_mediaTypeCache.TryGet(s, out var cached))
        {
            value = cached;
            return true;
        }

        if (MediaTypeHeaderValue.TryParse(s, out var parsedValue))
        {
            value = new AcceptMediaType(
                parsedValue.Type,
                parsedValue.SubType,
                parsedValue.Quality,
                parsedValue.Charset);
            s_mediaTypeCache.TryAdd(s, value);
            return true;
        }

        value = default;
        return false;
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

        public OperationResult? ErrorResult { get; }

        [MemberNotNullWhen(true, nameof(ErrorResult))]
        public bool HasError { get; }
    }
}
