using System.Collections.Frozen;

namespace HotChocolate.Types;

public static class HotChocolateTypesAbstractionsScalarDefinitionExtensions
{
    private static readonly FrozenDictionary<string, ScalarSerializationType> s_serializationTypeLookup =
        new Dictionary<string, ScalarSerializationType>
        {
            ["https://scalars.graphql.org/chillicream/date-time.html"] = ScalarSerializationType.String,
            ["https://scalars.graphql.org/chillicream/local-date.html"] = ScalarSerializationType.String
        }
        .ToFrozenDictionary();

    public static ScalarSerializationType GetScalarSerializationType(
        this IScalarTypeDefinition scalarTypeDefinition)
    {
        if (scalarTypeDefinition.SpecifiedBy is not null
            && s_serializationTypeLookup.TryGetValue(
                scalarTypeDefinition.SpecifiedBy.OriginalString,
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
