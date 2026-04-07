using HotChocolate.Fusion.Errors;
using HotChocolate.Language;
using static HotChocolate.Fusion.ApolloFederation.Properties.FederationResources;

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
            result.Errors.Add(new CompositionError(FederationSchemaAnalyzer_FederationV1NotSupported));
            return;
        }

        result.FederationVersion = federationVersion;
    }

    private static string? FindFederationVersion(DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            var directives = definition switch
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
                if (!directive.Name.Value.Equals(FederationDirectiveNames.Link, StringComparison.Ordinal))
                {
                    continue;
                }

                var url = GetStringArgument(directive, "url");

                if (url is null || !url.Contains(FederationUrlPrefix, StringComparison.Ordinal))
                {
                    continue;
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
    }

    private static void AnalyzeTypeDefinitions(DocumentNode document, AnalysisResult result)
    {
        var unsupportedDirectives = new HashSet<string>(StringComparer.Ordinal)
        {
            FederationDirectiveNames.ComposeDirective,
            FederationDirectiveNames.Authenticated,
            FederationDirectiveNames.RequiresScopes,
            FederationDirectiveNames.Policy,
            FederationDirectiveNames.InterfaceObject
        };

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

                case DirectiveDefinitionNode directiveDef
                    when unsupportedDirectives.Contains(directiveDef.Name.Value):
                    result.Errors.Add(new CompositionError(string.Format(
                        FederationSchemaAnalyzer_DirectiveNotSupported,
                        directiveDef.Name.Value)));
                    break;
            }
        }
    }

    private static void AnalyzeComplexType(
        string typeName,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<FieldDefinitionNode> fields,
        AnalysisResult result)
    {
        // we extract the key directives from federation and construct from resolvable keys lookups and from
        // non-resolvable keys simply the keys.
        foreach (var directive in directives)
        {
            if (!directive.Name.Value.Equals(FederationDirectiveNames.Key, StringComparison.Ordinal))
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

        // For later processing we need the type of each field of this type.
        var fieldTypes = new Dictionary<string, ITypeNode>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            fieldTypes[field.Name.Value] = field.Type;
        }

        result.TypeFieldTypes[typeName] = fieldTypes;
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
