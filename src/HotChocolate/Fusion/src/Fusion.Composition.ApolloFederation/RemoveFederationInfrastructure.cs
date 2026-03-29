using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes Apollo Federation infrastructure types, directives, and fields
/// from a schema document.
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

    private static readonly HashSet<string> _federationFieldNames = new(StringComparer.Ordinal)
    {
        FederationFieldNames.Entities,
        FederationFieldNames.Service
    };

    /// <summary>
    /// Applies the transformation to remove federation infrastructure from the document.
    /// </summary>
    /// <param name="document">
    /// The document to transform.
    /// </param>
    /// <param name="analysis">
    /// The analysis result containing metadata about the schema.
    /// </param>
    /// <returns>
    /// A new document with federation infrastructure removed.
    /// </returns>
    public static DocumentNode Apply(DocumentNode document, AnalysisResult analysis)
    {
        var definitions = new List<IDefinitionNode>(document.Definitions.Count);

        foreach (var definition in document.Definitions)
        {
            var transformed = TransformDefinition(definition, analysis);

            if (transformed is not null)
            {
                definitions.Add(transformed);
            }
        }

        return document.WithDefinitions(definitions);
    }

    private static IDefinitionNode? TransformDefinition(
        IDefinitionNode definition,
        AnalysisResult analysis)
    {
        switch (definition)
        {
            case ObjectTypeDefinitionNode objectType
                when objectType.Name.Value.Equals(
                    FederationTypeNames.Service,
                    StringComparison.Ordinal):
                return null;

            case UnionTypeDefinitionNode unionType
                when unionType.Name.Value.Equals(
                    FederationTypeNames.Entity,
                    StringComparison.Ordinal):
                return null;

            case ScalarTypeDefinitionNode scalarType
                when _federationScalarNames.Contains(scalarType.Name.Value):
                return null;

            case DirectiveDefinitionNode directiveDef
                when _federationDirectiveNames.Contains(directiveDef.Name.Value):
                return null;

            case SchemaDefinitionNode schemaDef:
                return TransformSchemaDefinition(schemaDef);

            case SchemaExtensionNode schemaExt:
                return TransformSchemaExtension(schemaExt);

            case ObjectTypeDefinitionNode objectType
                when objectType.Name.Value.Equals(
                    analysis.QueryTypeName,
                    StringComparison.Ordinal):
                return RemoveQueryFederationFields(objectType);

            default:
                return definition;
        }
    }

    private static IDefinitionNode? TransformSchemaDefinition(SchemaDefinitionNode schemaDef)
    {
        var nonLinkDirectives = new List<DirectiveNode>();

        foreach (var directive in schemaDef.Directives)
        {
            if (!directive.Name.Value.Equals(
                    FederationDirectiveNames.Link,
                    StringComparison.Ordinal))
            {
                nonLinkDirectives.Add(directive);
            }
        }

        // If the schema definition only had @link directives and has standard
        // operation types, remove it entirely.
        if (nonLinkDirectives.Count == 0 && HasOnlyStandardOperationTypes(schemaDef))
        {
            return null;
        }

        // Otherwise keep it but strip the @link directives.
        if (nonLinkDirectives.Count != schemaDef.Directives.Count)
        {
            return schemaDef.WithDirectives(nonLinkDirectives);
        }

        return schemaDef;
    }

    private static IDefinitionNode? TransformSchemaExtension(SchemaExtensionNode schemaExt)
    {
        var nonLinkDirectives = new List<DirectiveNode>();

        foreach (var directive in schemaExt.Directives)
        {
            if (!directive.Name.Value.Equals(
                    FederationDirectiveNames.Link,
                    StringComparison.Ordinal))
            {
                nonLinkDirectives.Add(directive);
            }
        }

        // If only @link directives, remove entirely.
        if (nonLinkDirectives.Count == 0)
        {
            return null;
        }

        if (nonLinkDirectives.Count != schemaExt.Directives.Count)
        {
            return schemaExt.WithDirectives(nonLinkDirectives);
        }

        return schemaExt;
    }

    private static bool HasOnlyStandardOperationTypes(SchemaDefinitionNode schemaDef)
    {
        foreach (var operationType in schemaDef.OperationTypes)
        {
            var isStandard = operationType.Operation switch
            {
                OperationType.Query
                    => operationType.Type.Name.Value.Equals("Query", StringComparison.Ordinal),
                OperationType.Mutation
                    => operationType.Type.Name.Value.Equals("Mutation", StringComparison.Ordinal),
                OperationType.Subscription
                    => operationType.Type.Name.Value.Equals(
                        "Subscription",
                        StringComparison.Ordinal),
                _ => false
            };

            if (!isStandard)
            {
                return false;
            }
        }

        return true;
    }

    private static ObjectTypeDefinitionNode RemoveQueryFederationFields(
        ObjectTypeDefinitionNode queryType)
    {
        var filteredFields = new List<FieldDefinitionNode>(queryType.Fields.Count);

        foreach (var field in queryType.Fields)
        {
            if (!_federationFieldNames.Contains(field.Name.Value))
            {
                filteredFields.Add(field);
            }
        }

        if (filteredFields.Count == queryType.Fields.Count)
        {
            return queryType;
        }

        return queryType.WithFields(filteredFields);
    }
}
