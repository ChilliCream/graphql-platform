using HotChocolate.Fusion.Definitions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

internal static class FederationV1DirectiveDefinitions
{
    public static void Apply(MutableSchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
            WellKnownTypeNames.FieldSelectionSet,
            out var fieldSelectionSetType))
        {
            return;
        }

        Replace(schema, CreateKey(fieldSelectionSetType));
        Replace(schema, new RequiresMutableDirectiveDefinition(fieldSelectionSetType));
        Replace(schema, CreateExternal());
        Replace(schema, CreateExtends());
    }

    private static MutableDirectiveDefinition CreateKey(
        MutableScalarTypeDefinition fieldSelectionSetType)
    {
        var definition = new MutableDirectiveDefinition(FederationDirectiveNames.Key)
        {
            IsRepeatable = true,
            Locations = DirectiveLocation.Object | DirectiveLocation.Interface
        };

        definition.Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Fields,
                new NonNullType(fieldSelectionSetType)));
        return definition;
    }

    private static MutableDirectiveDefinition CreateExternal()
        => new(FederationDirectiveNames.External)
        {
            Locations = DirectiveLocation.FieldDefinition
        };

    private static MutableDirectiveDefinition CreateExtends()
        => new(FederationDirectiveNames.Extends)
        {
            Locations = DirectiveLocation.Object | DirectiveLocation.Interface
        };

    private static void Replace(
        MutableSchemaDefinition schema,
        MutableDirectiveDefinition definition)
    {
        schema.DirectiveDefinitions.Remove(definition.Name);
        schema.DirectiveDefinitions.Add(definition);
    }
}
