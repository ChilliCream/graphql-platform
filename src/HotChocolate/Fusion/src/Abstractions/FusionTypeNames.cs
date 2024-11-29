using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion;

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
        string nodeDirective,
        string reEncodeIdDirective,
        string transportDirective,
        string fusionDirective,
        string internalDirective,
        string renameDirective,
        string removeDirective,
        string lookupDirective,
        string requireDirective,
        string selectionScalar,
        string selectionSetScalar,
        string typeNameScalar,
        string typeScalar,
        string uriScalar,
        string argumentDefinition,
        string resolverKind)
    {
        Prefix = prefix;
        VariableDirective = variableDirective;
        ResolverDirective = fetchDirective;
        SourceDirective = sourceDirective;
        IsDirective = isDirective;
        NodeDirective = nodeDirective;
        ReEncodeIdDirective = reEncodeIdDirective;
        TransportDirective = transportDirective;
        FusionDirective = fusionDirective;
        InternalDirective = internalDirective;
        RenameDirective = renameDirective;
        RemoveDirective = removeDirective;
        LookupDirective = lookupDirective;
        RequireDirective = requireDirective;
        SelectionScalar = selectionScalar;
        SelectionSetScalar = selectionSetScalar;
        TypeNameScalar = typeNameScalar;
        TypeScalar = typeScalar;
        UriScalar = uriScalar;
        ArgumentDefinition = argumentDefinition;
        ResolverKind = resolverKind;

        _fusionDirectives.Add(variableDirective);
        _fusionDirectives.Add(fetchDirective);
        _fusionDirectives.Add(sourceDirective);
        _fusionDirectives.Add(isDirective);
        _fusionDirectives.Add(nodeDirective);
        _fusionDirectives.Add(reEncodeIdDirective);
        _fusionDirectives.Add(transportDirective);
        _fusionDirectives.Add(fusionDirective);

        _fusionDirectives.Add(internalDirective);
        _fusionDirectives.Add(renameDirective);
        _fusionDirectives.Add(removeDirective);
        _fusionDirectives.Add(lookupDirective);
        _fusionDirectives.Add(requireDirective);

        _fusionTypes.Add(selectionScalar);
        _fusionTypes.Add(selectionSetScalar);
        _fusionTypes.Add(typeNameScalar);
        _fusionTypes.Add(typeScalar);
        _fusionTypes.Add(uriScalar);
        _fusionTypes.Add(argumentDefinition);
        _fusionTypes.Add(resolverKind);
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
    /// Gets the name of the resolver directive.
    /// </summary>
    public string ResolverDirective { get; }

    /// <summary>
    /// Gets the name of the source directive.
    /// </summary>
    public string SourceDirective { get; }

    /// <summary>
    /// Gets the name of the is directive.
    /// </summary>
    public string IsDirective { get; }

    /// <summary>
    /// Gets the name of the node directive.
    /// </summary>
    public string NodeDirective { get; }

    /// <summary>
    /// Gets the name of the reEncodeId directive.
    /// </summary>
    public string ReEncodeIdDirective { get; }

    /// <summary>
    /// Gets the name of the transport directive.
    /// </summary>
    public string TransportDirective { get; }

    /// <summary>
    /// Gets the name of the fusion directive.
    /// </summary>
    public string FusionDirective { get; }

    public string InternalDirective { get; }

    public string RenameDirective { get; }

    public string RemoveDirective { get; }

    public string LookupDirective { get; }

    public string RequireDirective { get; }

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
    /// Gets the name of the URI type scalar.
    /// </summary>
    public string UriScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL type scalar.
    /// </summary>
    public string ArgumentDefinition { get; }

    /// <summary>
    /// Gets the name of the URI type scalar.
    /// </summary>
    public string ResolverKind { get; }

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

    /// <summary>
    /// Creates a new instance of <see cref="FusionTypeNames"/>.
    /// </summary>
    /// <param name="prefix">
    /// The prefix for the fusion types.
    /// </param>
    /// <param name="prefixSelf">
    /// Specifies if the fusion directive itself should be prefixed.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="FusionTypeNames"/>.
    /// </returns>
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
                $"{prefix}_{FusionTypeBaseNames.NodeDirective}",
                $"{prefix}_{FusionTypeBaseNames.ReEncodeIdDirective}",
                $"{prefix}_{FusionTypeBaseNames.TransportDirective}",
                prefixSelf
                    ? $"{prefix}_{FusionTypeBaseNames.FusionDirective}"
                    : FusionTypeBaseNames.FusionDirective,
                FusionTypeBaseNames.InternalDirective,
                FusionTypeBaseNames.RenameDirective,
                FusionTypeBaseNames.RemoveDirective,
                FusionTypeBaseNames.LookupDirective,
                FusionTypeBaseNames.RequireDirective,
                $"{prefix}_{FusionTypeBaseNames.Selection}",
                $"{prefix}_{FusionTypeBaseNames.SelectionSet}",
                $"{prefix}_{FusionTypeBaseNames.TypeName}",
                $"{prefix}_{FusionTypeBaseNames.Type}",
                $"{prefix}_{FusionTypeBaseNames.Uri}",
                $"{prefix}_{FusionTypeBaseNames.ArgumentDefinition}",
                $"{prefix}_{FusionTypeBaseNames.ResolverKind}");
        }

        return new FusionTypeNames(
            null,
            FusionTypeBaseNames.VariableDirective,
            FusionTypeBaseNames.ResolverDirective,
            FusionTypeBaseNames.SourceDirective,
            FusionTypeBaseNames.IsDirective,
            FusionTypeBaseNames.NodeDirective,
            FusionTypeBaseNames.ReEncodeIdDirective,
            FusionTypeBaseNames.TransportDirective,
            FusionTypeBaseNames.FusionDirective,
            FusionTypeBaseNames.InternalDirective,
            FusionTypeBaseNames.RenameDirective,
            FusionTypeBaseNames.RemoveDirective,
            FusionTypeBaseNames.LookupDirective,
            FusionTypeBaseNames.RequireDirective,
            $"_{FusionTypeBaseNames.Selection}",
            $"_{FusionTypeBaseNames.SelectionSet}",
            $"_{FusionTypeBaseNames.TypeName}",
            $"_{FusionTypeBaseNames.Type}",
            $"_{FusionTypeBaseNames.Uri}",
            $"_{FusionTypeBaseNames.ArgumentDefinition}",
            $"_{FusionTypeBaseNames.ResolverKind}");
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

    private static void TryGetPrefix(
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

                if (prefixSelfArg?.Value is BooleanValueNode { Value: true, })
                {
                    var prefixArg =
                        directive.Arguments.FirstOrDefault(
                            t => t.Name.Value.EqualsOrdinal("prefix"));

                    if (prefixArg?.Value is StringValueNode prefixVal &&
                        directive.Name.Value.EqualsOrdinal($"{prefixVal.Value}{prefixedFusionDir}"))
                    {
                        prefixSelf = true;
                        prefix = prefixVal.Value;
                        return;
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
                    return;
                }
            }
        }

        prefixSelf = false;
        prefix = null;
    }
}
