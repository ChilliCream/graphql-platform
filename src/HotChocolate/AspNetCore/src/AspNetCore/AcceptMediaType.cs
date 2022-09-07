using HotChocolate.Utilities;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore;

public readonly struct AcceptMediaType
{
    internal AcceptMediaType(
        StringSegment type,
        StringSegment subType,
        double? quality,
        StringSegment charset)
    {
        Type = type.Value;
        SubType = subType.Value;
        Quality = quality;
        Charset = charset.HasValue ? charset.Value : null;
        IsUtf8 = Charset?.Equals("utf-8", StringComparison.OrdinalIgnoreCase) ?? true;

        if (Type.EqualsOrdinal(ContentType.Types.All) && SubType.EqualsOrdinal(ContentType.Types.All))
        {
            Kind = AcceptMediaTypeKind.All;
        }
        else if (Type.EqualsOrdinal(ContentType.Types.Application))
        {
            if (SubType.EqualsOrdinal(ContentType.Types.All))
            {
                Kind = AcceptMediaTypeKind.AllApplication;
            }
            else if (SubType.EqualsOrdinal(ContentType.SubTypes.GraphQLResponse))
            {
                Kind = AcceptMediaTypeKind.ApplicationGraphQL;
            }
            else if (SubType.EqualsOrdinal(ContentType.SubTypes.Json))
            {
                Kind = AcceptMediaTypeKind.ApplicationJson;
            }
        }
        else if (Type.EqualsOrdinal(ContentType.Types.MultiPart))
        {
            if (SubType.EqualsOrdinal(ContentType.Types.All))
            {
                Kind = AcceptMediaTypeKind.AllMultiPart;
            }
            else if (SubType.EqualsOrdinal(ContentType.SubTypes.Mixed))
            {
                Kind = AcceptMediaTypeKind.MultiPartMixed;
            }
        }
        else if (Type.EqualsOrdinal(ContentType.Types.Text) && SubType.EqualsOrdinal(ContentType.SubTypes.EventStream))
        {
            Kind = AcceptMediaTypeKind.EventStream;
        }
        else
        {
            Kind = AcceptMediaTypeKind.Unknown;
        }
    }

    public AcceptMediaTypeKind Kind { get; }

    public string Type { get; }

    public string SubType { get; }

    public double? Quality { get; }

    public string? Charset { get; }

    public bool IsUtf8 { get; }
}
