using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Helper class that tracks the namespaced fusion types.
/// </summary>
public sealed class FusionTypeNames
{
    private readonly HashSet<string> _fusionTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _fusionDirectives = new(StringComparer.OrdinalIgnoreCase);

    private FusionTypeNames(
        string? prefix,
        string variableDirective,
        string fetchDirective,
        string sourceDirective,
        string isDirective,
        string httpDirective,
        string fusionDirective,
        string selectionScalar,
        string selectionSetScalar,
        string typeNameScalar,
        string typeScalar)
    {
        Prefix = prefix;
        VariableDirective = variableDirective;
        FetchDirective = fetchDirective;
        SourceDirective = sourceDirective;
        IsDirective = isDirective;
        HttpDirective = httpDirective;
        FusionDirective = fusionDirective;
        SelectionScalar = selectionScalar;
        SelectionSetScalar = selectionSetScalar;
        TypeNameScalar = typeNameScalar;
        TypeScalar = typeScalar;

        _fusionDirectives.Add(variableDirective);
        _fusionDirectives.Add(fetchDirective);
        _fusionDirectives.Add(sourceDirective);
        _fusionDirectives.Add(isDirective);
        _fusionDirectives.Add(httpDirective);
        _fusionDirectives.Add(fusionDirective);

        _fusionTypes.Add(selectionScalar);
        _fusionTypes.Add(selectionSetScalar);
        _fusionTypes.Add(typeNameScalar);
        _fusionTypes.Add(typeScalar);
    }

    /// <summary>
    /// Gets the prefix for the fusion types.
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string VariableDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string FetchDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string SourceDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string IsDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string HttpDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string FusionDirective { get; }

    /// <summary>
    /// Gets the name of the GraphQL selection scalar.
    /// </summary>
    public string SelectionScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL selection set scalar.
    /// </summary>
    public string SelectionSetScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL type name scalar.
    /// </summary>
    public string TypeNameScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL type scalar.
    /// </summary>
    public string TypeScalar { get; }

    /// <summary>
    /// Specifies if the <paramref name="directiveName"/> represents a fusion directive.
    /// </summary>
    /// <param name="directiveName">
    /// A directive name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="directiveName"/> represents a fusion directive;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsFusionDirective(string directiveName)
        => _fusionDirectives.Contains(directiveName);

    /// <summary>
    /// Specifies if the <paramref name="typeName"/> represents a fusion type.
    /// </summary>
    /// <param name="typeName">
    /// A directive name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="typeName"/> represents a fusion type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsFusionType(string typeName)
        => _fusionTypes.Contains(typeName);

    public static FusionTypeNames Create(string? prefix = null, bool prefixSelf = false)
    {
        if (prefix is not null)
        {
            return new FusionTypeNames(
                prefix,
                $"{prefix}_{FusionTypeBaseNames.VariableDirective}",
                $"{prefix}_{FusionTypeBaseNames.ResolverDirective}",
                $"{prefix}_{FusionTypeBaseNames.SourceDirective}",
                $"{prefix}_{FusionTypeBaseNames.IsDirective}",
                $"{prefix}_{FusionTypeBaseNames.HttpDirective}",
                prefixSelf
                    ? $"{prefix}_{FusionTypeBaseNames.FusionDirective}"
                    : FusionTypeBaseNames.FusionDirective,
                $"{prefix}_{FusionTypeBaseNames.Selection}",
                $"{prefix}_{FusionTypeBaseNames.SelectionSet}",
                $"{prefix}_{FusionTypeBaseNames.TypeName}",
                $"{prefix}_{FusionTypeBaseNames.Type}");
        }

        return new FusionTypeNames(
            null,
            FusionTypeBaseNames.VariableDirective,
            FusionTypeBaseNames.ResolverDirective,
            FusionTypeBaseNames.SourceDirective,
            FusionTypeBaseNames.IsDirective,
            FusionTypeBaseNames.HttpDirective,
            FusionTypeBaseNames.FusionDirective,
            FusionTypeBaseNames.Selection,
            FusionTypeBaseNames.SelectionSet,
            FusionTypeBaseNames.TypeName,
            FusionTypeBaseNames.Type);
    }

    public static FusionTypeNames From(DocumentNode document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var schemaDef = document.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();

        if (schemaDef is null)
        {
            throw new ArgumentException(
                FusionAbstractionResources.FusionTypeNames_NoSchemaDef,
                nameof(document));
        }

        TryGetPrefix(schemaDef.Directives, out var prefixSelf, out var prefix);
        return Create(prefix, prefixSelf);
    }

    private static bool TryGetPrefix(
        IReadOnlyList<DirectiveNode> schemaDirectives,
        out bool prefixSelf,
        [NotNullWhen(true)] out string? prefix)
    {
        const string prefixedFusionDir = "_" + FusionTypeBaseNames.FusionDirective;

        foreach (var directive in schemaDirectives)
        {
            if (directive.Name.Value.EndsWith(prefixedFusionDir, StringComparison.Ordinal))
            {
                var prefixSelfArg =
                    directive.Arguments.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal("prefixSelf"));

                if (prefixSelfArg?.Value is BooleanValueNode { Value: true })
                {
                    var prefixArg =
                        directive.Arguments.FirstOrDefault(
                            t => t.Name.Value.EqualsOrdinal("prefix"));

                    if (prefixArg?.Value is StringValueNode prefixVal &&
                        directive.Name.Value.EqualsOrdinal($"{prefixVal.Value}{prefixedFusionDir}"))
                    {
                        prefixSelf = true;
                        prefix = prefixVal.Value;
                        return true;
                    }
                }
            }
        }

        foreach (var directive in schemaDirectives)
        {
            if (directive.Name.Value.EqualsOrdinal(FusionTypeBaseNames.FusionDirective))
            {
                var prefixArg =
                    directive.Arguments.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal("prefix"));

                if (prefixArg?.Value is StringValueNode prefixVal)
                {
                    prefixSelf = false;
                    prefix = prefixVal.Value;
                    return true;
                }
            }
        }

        prefixSelf = false;
        prefix = null;
        return false;
    }
}
