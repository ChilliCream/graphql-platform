namespace HotChocolate.Types;

public static class HotChocolateTypesAbstractionsScalarDefinitionExtensions
{
    public static ScalarSerializationType GetScalarSerializationType(
        this IScalarTypeDefinition scalarTypeDefinition)
    {
        // TODO: Handle specifiedBy

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
