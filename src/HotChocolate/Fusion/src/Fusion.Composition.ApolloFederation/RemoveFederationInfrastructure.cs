using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes Apollo Federation infrastructure types, directives, and fields
/// from a mutable schema definition.
/// </summary>
internal static class RemoveFederationInfrastructure
{
    private static readonly HashSet<string> _federationDirectiveNames = new(StringComparer.Ordinal)
    {
        FederationDirectiveNames.Key,
        FederationDirectiveNames.Requires,
        FederationDirectiveNames.Provides,
        FederationDirectiveNames.External,
        FederationDirectiveNames.Link,
        FederationDirectiveNames.Shareable,
        FederationDirectiveNames.Inaccessible,
        FederationDirectiveNames.Override,
        FederationDirectiveNames.Tag,
        FederationDirectiveNames.InterfaceObject,
        FederationDirectiveNames.ComposeDirective,
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes,
        FederationDirectiveNames.Policy
    };

    private static readonly HashSet<string> _federationScalarNames = new(StringComparer.Ordinal)
    {
        FederationTypeNames.Any,
        FederationTypeNames.FieldSet,
        FederationTypeNames.LegacyFieldSet
    };

    /// <summary>
    /// Applies the transformation to remove federation infrastructure from the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        // Remove federation directive definitions.
        foreach (var name in _federationDirectiveNames)
        {
            schema.DirectiveDefinitions.Remove(name);
        }

        // Remove federation scalar types.
        foreach (var name in _federationScalarNames)
        {
            schema.Types.Remove(name);
        }

        // Remove _Service type and _Entity union.
        schema.Types.Remove(FederationTypeNames.Service);
        schema.Types.Remove(FederationTypeNames.Entity);

        // Remove _entities and _service fields from query type.
        if (schema.QueryType is not null)
        {
            schema.QueryType.Fields.Remove(FederationFieldNames.Entities);
            schema.QueryType.Fields.Remove(FederationFieldNames.Service);
        }

        // Remove @link directives from schema.
        var linkDirectives = schema.Directives[FederationDirectiveNames.Link].ToList();

        foreach (var directive in linkDirectives)
        {
            schema.Directives.Remove(directive);
        }
    }
}
