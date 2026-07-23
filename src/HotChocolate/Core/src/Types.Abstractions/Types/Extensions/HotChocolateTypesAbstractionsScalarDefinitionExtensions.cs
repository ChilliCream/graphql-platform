using System.Collections.Frozen;

namespace HotChocolate.Types;

public static class HotChocolateTypesAbstractionsScalarDefinitionExtensions
{
    private static readonly FrozenDictionary<string, ScalarSerializationType> s_serializationTypeLookup =
        new Dictionary<string, ScalarSerializationType>
        {
            ["https://scalars.graphql.org/chillicream/any.html"] = ScalarSerializationType.Any,
            ["https://scalars.graphql.org/chillicream/base64-string.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/byte.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/date-time.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/date.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/decimal.html"] = ScalarSerializationType.Float,
            ["https://scalars.graphql.org/chillicream/duration.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/local-date-time.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/local-date.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/local-time.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/long.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/short.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/unsigned-byte.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/unsigned-int.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/unsigned-long.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/unsigned-short.html"] = ScalarSerializationType.Int,
            ["https://scalars.graphql.org/chillicream/uri.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/url.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/uuid.html"] = ScalarSerializationType.String
        }
        .ToFrozenDictionary();

    public static ScalarSerializationType GetScalarSerializationType(
        this IScalarTypeDefinition scalarTypeDefinition)
    {
        if (scalarTypeDefinition.SpecifiedBy is not null
            && s_serializationTypeLookup.TryGetValue(
                scalarTypeDefinition.SpecifiedBy,
                out var scalarSerializationType))
        {
            return scalarSerializationType;
        }

        return scalarTypeDefinition.Name switch
        {
            SpecScalarNames.String.Name => ScalarSerializationType.String,
            SpecScalarNames.Int.Name => ScalarSerializationType.Int,
            SpecScalarNames.Float.Name => ScalarSerializationType.Float,
            SpecScalarNames.Boolean.Name => ScalarSerializationType.Boolean,
            SpecScalarNames.ID.Name => ScalarSerializationType.String | ScalarSerializationType.Int,
            _ => scalarTypeDefinition.SerializationType
        };
    }
}
