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

    private FusionTypeNames(FusionTypeNamesConfig config)
    {
        Prefix = config.Prefix;
        VariableDirective = config.VariableDirective;
        ResolverDirective = config.ResolverDirective;
        SourceDirective = config.SourceDirective;
        IsDirective = config.IsDirective;
        ResolveDirective = config.ResolveDirective;
        NodeDirective = config.NodeDirective;
        ReEncodeIdDirective = config.ReEncodeIdDirective;
        TransportDirective = config.TransportDirective;
        PrivateDirective = config.PrivateDirective;
        FusionDirective = config.FusionDirective;
        SelectionScalar = config.SelectionScalar;
        SelectionSetScalar = config.SelectionSetScalar;
        TypeNameScalar = config.TypeNameScalar;
        TypeScalar = config.TypeScalar;
        UriScalar = config.UriScalar;
        ArgumentDefinition = config.ArgumentDefinition;
        ResolverKind = config.ResolverKind;
        DeclareDirective = config.DeclareDirective;

        _fusionDirectives.Add(config.PrivateDirective);
        _fusionDirectives.Add(config.VariableDirective);
        _fusionDirectives.Add(config.ResolverDirective);
        _fusionDirectives.Add(config.SourceDirective);
        _fusionDirectives.Add(config.IsDirective);
        _fusionDirectives.Add(config.NodeDirective);
        _fusionDirectives.Add(config.ReEncodeIdDirective);
        _fusionDirectives.Add(config.TransportDirective);
        _fusionDirectives.Add(config.FusionDirective);
        _fusionDirectives.Add(config.DeclareDirective);

        _fusionTypes.Add(config.SelectionScalar);
        _fusionTypes.Add(config.SelectionSetScalar);
        _fusionTypes.Add(config.TypeNameScalar);
        _fusionTypes.Add(config.TypeScalar);
        _fusionTypes.Add(config.UriScalar);
        _fusionTypes.Add(config.ArgumentDefinition);
        _fusionTypes.Add(config.ResolverKind);
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
    /// Gets the name of the resolve directive.
    /// </summary>
    public string ResolveDirective { get; }

    /// <summary>
    /// Gets the name of the node directive.
    /// </summary>
    public string NodeDirective { get; }
    
    /// <summary>
    /// Gets the name of the declare directive.
    /// </summary>
    public string DeclareDirective { get; }

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
    /// Gets the name of the private directive.
    /// </summary>
    public string PrivateDirective { get; private set; }

    public string RemoveDirective { get; private set; }

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
                new FusionTypeNamesConfig
                {
                    Prefix = prefix,
                    VariableDirective = $"{prefix}_{FusionTypeBaseNames.VariableDirective}",
                    ResolverDirective = $"{prefix}_{FusionTypeBaseNames.ResolverDirective}",
                    SourceDirective = $"{prefix}_{FusionTypeBaseNames.SourceDirective}",
                    IsDirective = $"{prefix}_{FusionTypeBaseNames.IsDirective}",
                    ResolveDirective = $"{prefix}_{FusionTypeBaseNames.ResolveDirective}",
                    NodeDirective = $"{prefix}_{FusionTypeBaseNames.NodeDirective}",
                    ReEncodeIdDirective = $"{prefix}_{FusionTypeBaseNames.ReEncodeIdDirective}",
                    TransportDirective = $"{prefix}_{FusionTypeBaseNames.TransportDirective}",
                    FusionDirective = prefixSelf
                        ? $"{prefix}_{FusionTypeBaseNames.FusionDirective}"
                        : FusionTypeBaseNames.FusionDirective,
                    SelectionScalar = $"{prefix}_{FusionTypeBaseNames.Selection}",
                    SelectionSetScalar = $"{prefix}_{FusionTypeBaseNames.SelectionSet}",
                    TypeNameScalar = $"{prefix}_{FusionTypeBaseNames.Name}",
                    TypeScalar = $"{prefix}_{FusionTypeBaseNames.Type}",
                    UriScalar = $"{prefix}_{FusionTypeBaseNames.Uri}",
                    ArgumentDefinition = $"{prefix}_{FusionTypeBaseNames.ArgumentDefinition}",
                    ResolverKind = $"{prefix}_{FusionTypeBaseNames.ResolverKind}",
                    PrivateDirective = $"{prefix}_{FusionTypeBaseNames.PrivateDirective}",
                    DeclareDirective = $"{prefix}_{FusionTypeBaseNames.DeclareDirective}",
                });
        }

        return new FusionTypeNames(
            new FusionTypeNamesConfig
            {
                VariableDirective = FusionTypeBaseNames.VariableDirective,
                ResolverDirective = FusionTypeBaseNames.ResolverDirective,
                SourceDirective = FusionTypeBaseNames.SourceDirective,
                IsDirective = FusionTypeBaseNames.IsDirective,
                ResolveDirective = FusionTypeBaseNames.ResolveDirective,
                NodeDirective = FusionTypeBaseNames.NodeDirective,
                ReEncodeIdDirective = FusionTypeBaseNames.ReEncodeIdDirective,
                TransportDirective = FusionTypeBaseNames.TransportDirective,
                FusionDirective = FusionTypeBaseNames.FusionDirective,
                SelectionScalar = FusionTypeBaseNames.Selection,
                SelectionSetScalar = FusionTypeBaseNames.SelectionSet,
                TypeNameScalar = FusionTypeBaseNames.Name,
                TypeScalar = FusionTypeBaseNames.Type,
                UriScalar = FusionTypeBaseNames.Uri,
                ArgumentDefinition = FusionTypeBaseNames.ArgumentDefinition,
                ResolverKind = FusionTypeBaseNames.ResolverKind,
                PrivateDirective = FusionTypeBaseNames.PrivateDirective,
                DeclareDirective = FusionTypeBaseNames.DeclareDirective
            });
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
    
    private ref struct FusionTypeNamesConfig
    {
        public string? Prefix { get; set; }
        public string VariableDirective { get; set; }
        public string ResolverDirective { get; set; }
        public string SourceDirective { get; set; }
        public string IsDirective { get; set; }
        public string ResolveDirective { get; set; }
        public string DeclareDirective { get; set; }
        public string NodeDirective { get; set; }
        public string ReEncodeIdDirective { get; set; }
        public string TransportDirective { get; set; }
        public string FusionDirective { get; set; }
        public string SelectionScalar { get; set; }
        public string SelectionSetScalar { get; set; }
        public string TypeNameScalar { get; set; }
        public string TypeScalar { get; set; }
        public string UriScalar { get; set; }
        public string ArgumentDefinition { get; set; }
        public string ResolverKind { get; set; }
        public string PrivateDirective { get; set; }
    }
}
