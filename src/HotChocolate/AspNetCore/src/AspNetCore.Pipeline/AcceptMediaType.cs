using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Representation of a single media type entry from the accept header.
/// </summary>
public readonly struct AcceptMediaType
{
    private const string Utf8 = "utf-8";

    /// <summary>
    /// Initializes a new instance of <see cref="AcceptMediaType"/>.
    /// </summary>
    /// <param name="type">
    /// The type of the media type header entry.
    /// </param>
    /// <param name="subType">
    /// The subtype of the media type header entry.
    /// </param>
    /// <param name="quality">
    /// The value of the quality parameter `q`.
    /// </param>
    /// <param name="charset">
    /// The charset.
    /// </param>
    /// <param name="incrementalDeliveryFormat">
    /// The incremental delivery format version.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Type or subtype are empty.
    /// </exception>
    internal AcceptMediaType(
        StringSegment type,
        StringSegment subType,
        double? quality,
        StringSegment charset,
        IncrementalDeliveryFormat incrementalDeliveryFormat = IncrementalDeliveryFormat.Undefined)
    {
        if (!type.HasValue)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (!subType.HasValue)
        {
            throw new ArgumentNullException(nameof(subType));
        }

        Kind = ResolveKind(type, subType);
        Type = ResolveType(type, Kind);
        SubType = ResolveSubType(subType, Kind);
        Quality = quality;
        Charset = ResolveCharset(charset);
        IncrementalDeliveryFormat = incrementalDeliveryFormat;
        IsUtf8 = Charset?.Equals(Utf8, StringComparison.OrdinalIgnoreCase) ?? true;
    }

    /// <summary>
    /// Gets the media type kind which is an enum representing well-known media type.
    /// </summary>
    public AcceptMediaTypeKind Kind { get; }

    /// <summary>
    /// Gets the type of the <see cref="AcceptMediaType"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/json"</c>,
    /// the property gives the value <c>"application"</c>.
    /// </example>
    /// <remarks>
    /// See <see href="https://tools.ietf.org/html/rfc6838#section-4.2"/>
    /// for more details on the type.
    /// </remarks>
    public string Type { get; }

    /// <summary>
    /// Gets the subtype of the <see cref="AcceptMediaType"/>.
    /// </summary>
    /// <example>
    /// For the media type <c>"application/vnd.example+json"</c>, the property gives the value
    /// <c>"vnd.example+json"</c>.
    /// </example>
    /// <remarks>
    /// See <see href="https://tools.ietf.org/html/rfc6838#section-4.2"/>
    /// for more details on the subtype.
    /// </remarks>
    public string SubType { get; }

    /// <summary>
    /// Gets or sets the value of the quality parameter. Returns null
    /// if there is no quality.
    /// </summary>
    public double? Quality { get; }

    /// <summary>
    /// Gets or sets the value of the charset parameter.
    /// Returns <c>null</c> if there is no charset.
    /// </summary>
    public string? Charset { get; }

    /// <summary>
    /// Gets the incremental delivery format version.
    /// </summary>
    public IncrementalDeliveryFormat IncrementalDeliveryFormat { get; }

    /// <summary>
    /// Defines if the charset is UTF-8.
    /// </summary>
    public bool IsUtf8 { get; }

    private static AcceptMediaTypeKind ResolveKind(StringSegment type, StringSegment subType)
    {
        if (type.Equals(ContentType.Types.All, StringComparison.Ordinal)
            && subType.Equals(ContentType.Types.All, StringComparison.Ordinal))
        {
            return AcceptMediaTypeKind.All;
        }

        if (type.Equals(ContentType.Types.Application, StringComparison.OrdinalIgnoreCase))
        {
            if (subType.Equals(ContentType.Types.All, StringComparison.Ordinal))
            {
                return AcceptMediaTypeKind.AllApplication;
            }

            if (subType.Equals(ContentType.SubTypes.GraphQLResponse, StringComparison.OrdinalIgnoreCase))
            {
                return AcceptMediaTypeKind.ApplicationGraphQL;
            }

            if (subType.Equals(ContentType.SubTypes.GraphQLResponseStream, StringComparison.OrdinalIgnoreCase))
            {
                return AcceptMediaTypeKind.ApplicationGraphQLStream;
            }

            if (subType.Equals(ContentType.SubTypes.Json, StringComparison.OrdinalIgnoreCase))
            {
                return AcceptMediaTypeKind.ApplicationJson;
            }

            if (subType.Equals(ContentType.SubTypes.JsonLines, StringComparison.OrdinalIgnoreCase))
            {
                return AcceptMediaTypeKind.ApplicationJsonLines;
            }
        }

        if (type.Equals(ContentType.Types.MultiPart, StringComparison.OrdinalIgnoreCase))
        {
            if (subType.Equals(ContentType.Types.All, StringComparison.Ordinal))
            {
                return AcceptMediaTypeKind.AllMultiPart;
            }

            if (subType.Equals(ContentType.SubTypes.Mixed, StringComparison.OrdinalIgnoreCase))
            {
                return AcceptMediaTypeKind.MultiPartMixed;
            }
        }

        if (type.Equals(ContentType.Types.Text, StringComparison.OrdinalIgnoreCase)
            && subType.Equals(ContentType.SubTypes.EventStream, StringComparison.OrdinalIgnoreCase))
        {
            return AcceptMediaTypeKind.EventStream;
        }

        return AcceptMediaTypeKind.Unknown;
    }

    private static string ResolveType(StringSegment type, AcceptMediaTypeKind kind)
        => kind switch
        {
            AcceptMediaTypeKind.All => ContentType.Types.All,
            AcceptMediaTypeKind.AllApplication
                or AcceptMediaTypeKind.ApplicationGraphQL
                or AcceptMediaTypeKind.ApplicationGraphQLStream
                or AcceptMediaTypeKind.ApplicationJson
                or AcceptMediaTypeKind.ApplicationJsonLines => ContentType.Types.Application,
            AcceptMediaTypeKind.AllMultiPart
                or AcceptMediaTypeKind.MultiPartMixed => ContentType.Types.MultiPart,
            AcceptMediaTypeKind.EventStream => ContentType.Types.Text,
            _ => type.Value!
        };

    private static string ResolveSubType(StringSegment subType, AcceptMediaTypeKind kind)
        => kind switch
        {
            AcceptMediaTypeKind.All
                or AcceptMediaTypeKind.AllApplication
                or AcceptMediaTypeKind.AllMultiPart => ContentType.Types.All,
            AcceptMediaTypeKind.ApplicationGraphQL => ContentType.SubTypes.GraphQLResponse,
            AcceptMediaTypeKind.ApplicationGraphQLStream => ContentType.SubTypes.GraphQLResponseStream,
            AcceptMediaTypeKind.ApplicationJson => ContentType.SubTypes.Json,
            AcceptMediaTypeKind.ApplicationJsonLines => ContentType.SubTypes.JsonLines,
            AcceptMediaTypeKind.MultiPartMixed => ContentType.SubTypes.Mixed,
            AcceptMediaTypeKind.EventStream => ContentType.SubTypes.EventStream,
            _ => subType.Value!
        };

    private static string? ResolveCharset(StringSegment charset)
    {
        if (!charset.HasValue)
        {
            return null;
        }

        return charset.Equals(Utf8, StringComparison.OrdinalIgnoreCase)
            ? Utf8
            : charset.Value;
    }
}
