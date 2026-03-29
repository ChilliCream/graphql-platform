using HotChocolate.Fusion.Errors;
using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Analyzes a parsed <see cref="DocumentNode"/> to extract Apollo Federation metadata
/// needed for transformations.
/// </summary>
internal static class FederationSchemaAnalyzer
{
    private const string FederationUrlPrefix = "specs.apollo.dev/federation";

    /// <summary>
    /// Analyzes the given federation schema document and extracts metadata.
    /// </summary>
    /// <param name="document">
    /// The parsed GraphQL document to analyze.
    /// </param>
    /// <returns>
    /// An <see cref="AnalysisResult"/> containing the extracted federation metadata.
    /// </returns>
    public static AnalysisResult Analyze(DocumentNode document)
    {
        var result = new AnalysisResult();

        DetectFederationVersion(document, result);
        DetectQueryTypeName(document, result);
        AnalyzeTypeDefinitions(document, result);

        return result;
    }

    private static void DetectFederationVersion(DocumentNode document, AnalysisResult result)
    {
        var federationVersion = FindFederationVersion(document);

        if (federationVersion is null)
        {
            result.Errors.Add(new CompositionError("Federation v1 is not supported."));
            return;
        }

        result.FederationVersion = federationVersion;
    }

    private static string? FindFederationVersion(DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            IReadOnlyList<DirectiveNode>? directives = definition switch
            {
                SchemaDefinitionNode schemaDef => schemaDef.Directives,
                SchemaExtensionNode schemaExt => schemaExt.Directives,
                _ => null
            };

            if (directives is null)
            {
                continue;
            }

            foreach (var directive in directives)
            {
                if (!directive.Name.Value.Equals(
                        FederationDirectiveNames.Link,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var url = GetStringArgument(directive, "url");

                if (url is null
                    || !url.Contains(FederationUrlPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                // Check for @composeDirective imports in this @link directive.
                if (HasComposeDirectiveImport(directive))
                {
                    // We only add the error but continue to extract the version.
                }

                // Extract version from URL like
                // "https://specs.apollo.dev/federation/v2.5"
                var lastSlash = url.LastIndexOf('/');

                if (lastSlash >= 0 && lastSlash < url.Length - 1)
                {
                    return url[(lastSlash + 1)..];
                }
            }
        }

        return null;
    }

    private static bool HasComposeDirectiveImport(DirectiveNode linkDirective)
    {
        // @link can import specific directives via the `import` argument.
        // We do not inspect imports here; @composeDirective is a separate directive.
        // This is checked elsewhere in AnalyzeTypeDefinitions.
        return false;
    }

    private static void DetectQueryTypeName(DocumentNode document, AnalysisResult result)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is not SchemaDefinitionNode schemaDef)
            {
                continue;
            }

            foreach (var operationType in schemaDef.OperationTypes)
            {
                if (operationType.Operation == OperationType.Query)
                {
                    result.QueryTypeName = operationType.Type.Name.Value;
                    return;
                }
            }
        }

        // Default remains "Query" as set in AnalysisResult constructor.
    }

    private static void AnalyzeTypeDefinitions(DocumentNode document, AnalysisResult result)
    {
        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode objectType:
                    AnalyzeComplexType(
                        objectType.Name.Value,
                        objectType.Directives,
                        objectType.Fields,
                        result);
                    break;

                case InterfaceTypeDefinitionNode interfaceType:
                    AnalyzeComplexType(
                        interfaceType.Name.Value,
                        interfaceType.Directives,
                        interfaceType.Fields,
                        result);
                    break;
            }
        }

        // Check for @composeDirective definitions in the document.
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode directiveDef
                && directiveDef.Name.Value.Equals(
                    FederationDirectiveNames.ComposeDirective,
                    StringComparison.Ordinal))
            {
                result.Errors.Add(
                    new CompositionError(
                        "The @composeDirective feature is not supported."));
                break;
            }
        }

        // Also check for @composeDirective usage in @link imports.
        foreach (var definition in document.Definitions)
        {
            IReadOnlyList<DirectiveNode>? directives = definition switch
            {
                SchemaDefinitionNode schemaDef => schemaDef.Directives,
                SchemaExtensionNode schemaExt => schemaExt.Directives,
                _ => null
            };

            if (directives is null)
            {
                continue;
            }

            foreach (var directive in directives)
            {
                if (directive.Name.Value.Equals(
                        FederationDirectiveNames.Link,
                        StringComparison.Ordinal))
                {
                    var url = GetStringArgument(directive, "url");

                    if (url?.Contains(FederationUrlPrefix, StringComparison.Ordinal) == false)
                    {
                        // This is a non-federation @link, check if there is a
                        // @composeDirective applied elsewhere.
                        CheckForComposeDirectiveUsage(document, result);
                        break;
                    }
                }
            }
        }
    }

    private static void CheckForComposeDirectiveUsage(
        DocumentNode document,
        AnalysisResult result)
    {
        foreach (var definition in document.Definitions)
        {
            IReadOnlyList<DirectiveNode>? directives = definition switch
            {
                SchemaDefinitionNode schemaDef => schemaDef.Directives,
                SchemaExtensionNode schemaExt => schemaExt.Directives,
                _ => null
            };

            if (directives is null)
            {
                continue;
            }

            foreach (var directive in directives)
            {
                if (directive.Name.Value.Equals(
                        FederationDirectiveNames.ComposeDirective,
                        StringComparison.Ordinal))
                {
                    result.Errors.Add(
                        new CompositionError(
                            "The @composeDirective feature is not supported."));
                    return;
                }
            }
        }
    }

    private static void AnalyzeComplexType(
        string typeName,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<FieldDefinitionNode> fields,
        AnalysisResult result)
    {
        // Check for unsupported directives on the type.
        foreach (var directive in directives)
        {
            if (directive.Name.Value.Equals(
                    FederationDirectiveNames.InterfaceObject,
                    StringComparison.Ordinal))
            {
                result.Errors.Add(
                    new CompositionError(
                        $"The @interfaceObject directive on type '{typeName}'"
                        + " is not supported."));
            }
        }

        // Extract @key directives.
        foreach (var directive in directives)
        {
            if (!directive.Name.Value.Equals(
                    FederationDirectiveNames.Key,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var fieldsValue = GetStringArgument(directive, "fields");

            if (fieldsValue is null)
            {
                continue;
            }

            var resolvable = GetBooleanArgument(directive, "resolvable") ?? true;

            if (!result.EntityKeys.TryGetValue(typeName, out var keyList))
            {
                keyList = [];
                result.EntityKeys[typeName] = keyList;
            }

            keyList.Add(new EntityKeyInfo
            {
                Fields = fieldsValue,
                Resolvable = resolvable
            });
        }

        // Build field type map.
        var fieldTypes = new Dictionary<string, ITypeNode>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            fieldTypes[field.Name.Value] = field.Type;
        }

        result.TypeFieldTypes[typeName] = fieldTypes;

        // Check for unsupported directives on fields.
        foreach (var field in fields)
        {
            foreach (var directive in field.Directives)
            {
                if (directive.Name.Value.Equals(
                        FederationDirectiveNames.InterfaceObject,
                        StringComparison.Ordinal))
                {
                    result.Errors.Add(
                        new CompositionError(
                            "The @interfaceObject directive on field"
                            + $" '{typeName}.{field.Name.Value}' is not supported."));
                }

                if (directive.Name.Value.Equals(
                        FederationDirectiveNames.Override,
                        StringComparison.Ordinal))
                {
                    var label = GetStringArgument(directive, "label");

                    if (label is not null)
                    {
                        result.Errors.Add(
                            new CompositionError(
                                "The @override directive with a 'label' argument"
                                + $" on field '{typeName}.{field.Name.Value}'"
                                + " is not supported."));
                    }
                }
            }
        }
    }

    private static string? GetStringArgument(DirectiveNode directive, string argumentName)
    {
        foreach (var argument in directive.Arguments)
        {
            if (argument.Name.Value.Equals(argumentName, StringComparison.Ordinal)
                && argument.Value is StringValueNode stringValue)
            {
                return stringValue.Value;
            }
        }

        return null;
    }

    private static bool? GetBooleanArgument(DirectiveNode directive, string argumentName)
    {
        foreach (var argument in directive.Arguments)
        {
            if (argument.Name.Value.Equals(argumentName, StringComparison.Ordinal)
                && argument.Value is BooleanValueNode boolValue)
            {
                return boolValue.Value;
            }
        }

        return null;
    }
}
