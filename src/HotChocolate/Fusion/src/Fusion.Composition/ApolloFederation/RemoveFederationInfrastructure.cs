using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes Apollo Federation infrastructure types, directives, and fields
/// from a mutable schema definition.
/// </summary>
internal static class RemoveFederationInfrastructure
{
    private static readonly HashSet<string> s_federationDirectiveNames =
    [
        with(StringComparer.Ordinal),
        FederationDirectiveNames.Key,
        FederationDirectiveNames.Requires,
        FederationDirectiveNames.Provides,
        FederationDirectiveNames.External,
        FederationDirectiveNames.Link,
        FederationDirectiveNames.Shareable,
        FederationDirectiveNames.Inaccessible,
        FederationDirectiveNames.Override,
        FederationDirectiveNames.Tag,
        FederationDirectiveNames.ComposeDirective,
        FederationDirectiveNames.Authenticated,
        FederationDirectiveNames.RequiresScopes,
        FederationDirectiveNames.Policy
    ];

    private static readonly HashSet<string> s_federationScalarNames =
    [
        with(StringComparer.Ordinal),
        FederationTypeNames.Any,
        FederationTypeNames.FieldSet,
        FederationTypeNames.LegacyFieldSet
    ];

    /// <summary>
    /// Applies the transformation to remove federation infrastructure from the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        // Remove federation directive definitions.
        foreach (var name in s_federationDirectiveNames)
        {
            schema.DirectiveDefinitions.Remove(name);
        }

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

        // Collect the type names still referenced after the federation directives and fields
        // above have been removed. Federation types such as FieldSet are exported vocabulary and
        // may be used by user-defined members (for example as a custom directive argument type).
        // Once referenced by a surviving member the type is part of the user's schema, so removing
        // it here would leave a dangling reference; those types are kept while unreferenced
        // federation types continue to be stripped.
        var referencedTypeNames = CollectReferencedTypeNames(schema);

        // Remove federation scalar types that are no longer referenced.
        foreach (var name in s_federationScalarNames)
        {
            if (!referencedTypeNames.Contains(name))
            {
                schema.Types.Remove(name);
            }
        }

        // Remove the _Service type and _Entity union when no longer referenced.
        if (!referencedTypeNames.Contains(FederationTypeNames.Service))
        {
            schema.Types.Remove(FederationTypeNames.Service);
        }

        if (!referencedTypeNames.Contains(FederationTypeNames.Entity))
        {
            schema.Types.Remove(FederationTypeNames.Entity);
        }
    }

    private static HashSet<string> CollectReferencedTypeNames(MutableSchemaDefinition schema)
    {
        var referencedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        referencedTypeNames.Add(field.Type.NamedType().Name);

                        foreach (var argument in field.Arguments)
                        {
                            referencedTypeNames.Add(argument.Type.NamedType().Name);
                        }
                    }

                    break;

                case MutableInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        referencedTypeNames.Add(field.Type.NamedType().Name);
                    }

                    break;
            }
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            foreach (var argument in directiveDefinition.Arguments)
            {
                referencedTypeNames.Add(argument.Type.NamedType().Name);
            }
        }

        return referencedTypeNames;
    }
}
