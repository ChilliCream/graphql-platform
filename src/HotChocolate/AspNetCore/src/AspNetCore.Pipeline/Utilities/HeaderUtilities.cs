using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    private const string IncrementalSpec = "incrementalSpec";
    private const string IncrementalSpecV01 = "v0.1";
    private const string IncrementalSpecV02 = "v0.2";
    private const string Charset = "charset";
    private const string Quality = "q";

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

            return ParseMultipleHeaderValues(value);
        }

        return new AcceptHeaderResult([]);
    }

    private static AcceptHeaderResult ParseHeaderValue(string headerValue)
    {
        if (!headerValue.Contains(','))
        {
            if (TryParseMediaType(headerValue, out var parsedValue))
            {
                return new AcceptHeaderResult([parsedValue]);
            }

            return new AcceptHeaderResult(headerValue);
        }

        return ParseMediaTypeList(headerValue);
    }

    private static AcceptHeaderResult ParseMultipleHeaderValues(StringValues values)
    {
        var parsedValues = ArrayPool<AcceptMediaType>.Shared.Rent(8);
        var count = 0;

        for (var i = 0; i < values.Count; i++)
        {
            var headerValue = values[i]!;

            if (!headerValue.Contains(','))
            {
                if (!TryParseMediaType(headerValue, out var parsedValue))
                {
                    ReturnBuffer(parsedValues);
                    return new AcceptHeaderResult(headerValue);
                }

                AddMediaType(ref parsedValues, ref count, parsedValue);
                continue;
            }

            if (!TryParseMediaTypeList(
                headerValue,
                ref parsedValues,
                ref count,
                out var invalidMediaType))
            {
                ReturnBuffer(parsedValues);
                return new AcceptHeaderResult(invalidMediaType ?? headerValue);
            }
        }

        if (count == 0)
        {
            ReturnBuffer(parsedValues);
            return new AcceptHeaderResult([]);
        }

        var result = new AcceptMediaType[count];
        parsedValues.AsSpan(0, count).CopyTo(result);
        ReturnBuffer(parsedValues);
        return new AcceptHeaderResult(result);
    }

    private static AcceptHeaderResult ParseMediaTypeList(string headerValue)
    {
        var parsedValues = ArrayPool<AcceptMediaType>.Shared.Rent(8);
        var count = 0;

        if (!TryParseMediaTypeList(
            headerValue,
            ref parsedValues,
            ref count,
            out var invalidMediaType))
        {
            ReturnBuffer(parsedValues);
            return new AcceptHeaderResult(invalidMediaType ?? headerValue);
        }

        if (count == 0)
        {
            ReturnBuffer(parsedValues);
            return new AcceptHeaderResult(headerValue);
        }

        var result = new AcceptMediaType[count];
        parsedValues.AsSpan(0, count).CopyTo(result);
        ReturnBuffer(parsedValues);
        return new AcceptHeaderResult(result);
    }

    private static bool TryParseMediaTypeList(
        string headerValue,
        ref AcceptMediaType[] parsedValues,
        ref int count,
        out string? invalidMediaType)
    {
        invalidMediaType = null;

        var span = headerValue.AsSpan();
        var position = 0;
        var expectValue = true;
        var hasValue = false;

        while (true)
        {
            SkipWhitespace(span, ref position);

            if (position >= span.Length)
            {
                if (expectValue || !hasValue)
                {
                    invalidMediaType = headerValue;
                    return false;
                }

                return true;
            }

            var segmentStart = position;
            var inQuotes = false;

            while (position < span.Length)
            {
                var c = span[position];

                if (c is '"' && !inQuotes)
                {
                    inQuotes = true;
                    position++;
                    continue;
                }

                if (c is '"' && inQuotes)
                {
                    inQuotes = false;
                    position++;
                    continue;
                }

                if (c is '\\' && inQuotes && position + 1 < span.Length)
                {
                    position += 2;
                    continue;
                }

                if (c is ',' && !inQuotes)
                {
                    break;
                }

                position++;
            }

            if (inQuotes)
            {
                invalidMediaType = headerValue;
                return false;
            }

            var segmentEnd = position;
            TrimWhitespace(span, ref segmentStart, ref segmentEnd);

            if (segmentStart == segmentEnd)
            {
                invalidMediaType = headerValue;
                return false;
            }

            if (!TryParseMediaType(headerValue, segmentStart, segmentEnd - segmentStart, out var parsedValue))
            {
                invalidMediaType = headerValue.AsSpan(segmentStart, segmentEnd - segmentStart).ToString();
                return false;
            }

            AddMediaType(ref parsedValues, ref count, parsedValue);

            hasValue = true;

            if (position >= span.Length)
            {
                return true;
            }

            // skip comma separator and continue with the next media type
            position++;
            expectValue = true;
        }
    }

    private static bool TryParseMediaType(string s, out AcceptMediaType value)
    {
        if (s_mediaTypeCache.TryGet(s, out var cached))
        {
            value = cached;
            return true;
        }

        if (TryParseMediaType(s, 0, s.Length, out value))
        {
            s_mediaTypeCache.TryAdd(s, value);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryParseMediaType(
        string source,
        int start,
        int length,
        out AcceptMediaType value)
    {
        value = default;

        var span = source.AsSpan(start, length);
        var position = 0;
        SkipWhitespace(span, ref position);

        if (position >= span.Length)
        {
            return false;
        }

        var typeStart = position;
        if (!ReadToken(span, ref position))
        {
            return false;
        }

        var typeEnd = position;
        if (position >= span.Length || span[position] is not '/')
        {
            return false;
        }

        position++;
        var subTypeStart = position;

        if (!ReadToken(span, ref position))
        {
            return false;
        }

        var subTypeEnd = position;
        var quality = (double?)null;
        var charset = StringSegment.Empty;
        var incrementalDeliveryFormat = IncrementalDeliveryFormat.Undefined;

        while (true)
        {
            SkipWhitespace(span, ref position);

            if (position >= span.Length)
            {
                break;
            }

            if (span[position] is not ';')
            {
                return false;
            }

            position++;
            SkipWhitespace(span, ref position);

            if (position >= span.Length)
            {
                return false;
            }

            var parameterNameStart = position;
            if (!ReadToken(span, ref position))
            {
                return false;
            }

            var parameterNameEnd = position;
            SkipWhitespace(span, ref position);

            if (position >= span.Length || span[position] is not '=')
            {
                return false;
            }

            position++;
            SkipWhitespace(span, ref position);

            if (position >= span.Length)
            {
                return false;
            }

            int parameterValueStart;
            int parameterValueEnd;

            if (span[position] is '"')
            {
                position++;
                parameterValueStart = position;

                while (position < span.Length)
                {
                    if (span[position] is '\\' && position + 1 < span.Length)
                    {
                        position += 2;
                        continue;
                    }

                    if (span[position] is '"')
                    {
                        break;
                    }

                    position++;
                }

                if (position >= span.Length || span[position] is not '"')
                {
                    return false;
                }

                parameterValueEnd = position;
                position++;
            }
            else
            {
                parameterValueStart = position;

                if (!ReadToken(span, ref position))
                {
                    return false;
                }

                parameterValueEnd = position;
            }

            var parameterName = span[parameterNameStart..parameterNameEnd];
            var parameterValue = span[parameterValueStart..parameterValueEnd];

            if (parameterName.Equals(Quality.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                if (!double.TryParse(
                    parameterValue,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var parsedQuality)
                    || parsedQuality is < 0 or > 1)
                {
                    return false;
                }

                quality = parsedQuality;
            }
            else if (parameterName.Equals(Charset.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                charset = new StringSegment(
                    source,
                    start + parameterValueStart,
                    parameterValueEnd - parameterValueStart);
            }
            else if (parameterName.Equals(IncrementalSpec.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                incrementalDeliveryFormat = ParseIncrementalDeliveryFormat(parameterValue);
            }
        }

        SkipWhitespace(span, ref position);

        if (position != span.Length)
        {
            return false;
        }

        value = new AcceptMediaType(
            new StringSegment(source, start + typeStart, typeEnd - typeStart),
            new StringSegment(source, start + subTypeStart, subTypeEnd - subTypeStart),
            quality,
            charset,
            incrementalDeliveryFormat);
        return true;
    }

    private static IncrementalDeliveryFormat ParseIncrementalDeliveryFormat(ReadOnlySpan<char> value)
    {
        if (value.Equals(IncrementalSpecV01.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return IncrementalDeliveryFormat.Version_0_1;
        }

        if (value.Equals(IncrementalSpecV02.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return IncrementalDeliveryFormat.Version_0_2;
        }

        return IncrementalDeliveryFormat.Undefined;
    }

    private static void AddMediaType(
        ref AcceptMediaType[] parsedValues,
        ref int count,
        AcceptMediaType value)
    {
        if (count >= parsedValues.Length)
        {
            var newBuffer = ArrayPool<AcceptMediaType>.Shared.Rent(parsedValues.Length * 2);
            parsedValues.AsSpan(0, count).CopyTo(newBuffer);
            ArrayPool<AcceptMediaType>.Shared.Return(parsedValues);
            parsedValues = newBuffer;
        }

        parsedValues[count++] = value;
    }

    private static void ReturnBuffer(AcceptMediaType[] buffer)
        => ArrayPool<AcceptMediaType>.Shared.Return(buffer);

    private static void SkipWhitespace(ReadOnlySpan<char> value, ref int position)
    {
        while (position < value.Length && char.IsWhiteSpace(value[position]))
        {
            position++;
        }
    }

    private static void TrimWhitespace(ReadOnlySpan<char> value, ref int start, ref int end)
    {
        while (start < end && char.IsWhiteSpace(value[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(value[end - 1]))
        {
            end--;
        }
    }

    private static bool ReadToken(ReadOnlySpan<char> value, ref int position)
    {
        var start = position;

        while (position < value.Length && IsTokenCharacter(value[position]))
        {
            position++;
        }

        return position > start;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTokenCharacter(char c)
        => c is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.'
            or '^' or '_' or '`' or '|' or '~';

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
