using System.Diagnostics.CodeAnalysis;
using System.Text;
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

    private FusionTypeNames(string? prefix = null, bool prefixSelf = false)
    {
        Prefix = prefix;

        var prefix0 = !string.IsNullOrEmpty(prefix)
            ? $"{prefix}_"
            : string.Empty;
        var prefix1 = !string.IsNullOrEmpty(prefix) && prefixSelf
            ? $"{prefix}_"
            : string.Empty;

        // Scalars
        ArgumentDefinition = $"{prefix0}{FusionTypeBaseNames.ArgumentDefinition}";
        NameScalar = $"{prefix0}{FusionTypeBaseNames.Name}";
        ResolverKindEnum = $"{prefix0}{FusionTypeBaseNames.ResolverKind}";
        SchemaCoordinateScalar= $"{prefix0}{FusionTypeBaseNames.SchemaCoordinate}";
        SelectionScalar = $"{prefix0}{FusionTypeBaseNames.Selection}";
        SelectionSetScalar = $"{prefix0}{FusionTypeBaseNames.SelectionSet}";
        TypeScalar = $"{prefix0}{FusionTypeBaseNames.Type}";
        UriScalar = $"{prefix0}{FusionTypeBaseNames.Uri}";
        OperationDefinitionScalar = $"{prefix0}{FusionTypeBaseNames.OperationDefinition}";

        _fusionTypes.Add(ArgumentDefinition);
        _fusionTypes.Add(NameScalar);
        _fusionTypes.Add(ResolverKindEnum);
        _fusionTypes.Add(SelectionScalar);
        _fusionTypes.Add(SelectionSetScalar);
        _fusionTypes.Add(TypeScalar);
        _fusionTypes.Add(UriScalar);

        // Directives
        DeclareDirective = $"{prefix0}{FusionTypeBaseNames.DeclareDirective}";
        FusionDirective = $"{prefix1}{FusionTypeBaseNames.FusionDirective}";
        IsDirective = $"{prefix0}{FusionTypeBaseNames.IsDirective}";
        NodeDirective = $"{prefix0}{FusionTypeBaseNames.NodeDirective}";
        PrivateDirective = $"{prefix0}{FusionTypeBaseNames.PrivateDirective}";
        RemoveDirective = $"{prefix0}{FusionTypeBaseNames.RemoveDirective}";
        RenameDirective = $"{prefix0}{FusionTypeBaseNames.RenameDirective}";
        RequireDirective = $"{prefix0}{FusionTypeBaseNames.RequireDirective}";
        ResolverDirective = $"{prefix0}{FusionTypeBaseNames.ResolverDirective}";
        ResolveDirective = $"{prefix0}{FusionTypeBaseNames.ResolveDirective}";
        SourceDirective = $"{prefix0}{FusionTypeBaseNames.SourceDirective}";
        TransportDirective = $"{prefix0}{FusionTypeBaseNames.TransportDirective}";
        VariableDirective = $"{prefix0}{FusionTypeBaseNames.VariableDirective}";

        _fusionDirectives.Add(DeclareDirective);
        _fusionDirectives.Add(FusionDirective);
        _fusionDirectives.Add(IsDirective);
        _fusionDirectives.Add(NodeDirective);
        _fusionDirectives.Add(PrivateDirective);
        _fusionDirectives.Add(RemoveDirective);
        _fusionDirectives.Add(RenameDirective);
        _fusionDirectives.Add(RequireDirective);
        _fusionDirectives.Add(ResolverDirective);
        _fusionDirectives.Add(ResolveDirective);
        _fusionDirectives.Add(SourceDirective);
        _fusionDirectives.Add(TransportDirective);
        _fusionDirectives.Add(VariableDirective);
    }

    /// <summary>
    /// Gets the prefix for the fusion types.
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// Gets the name of the GraphQL type scalar.
    /// </summary>
    public string ArgumentDefinition { get; }

    /// <summary>
    /// Gets the name of the GraphQL type name scalar.
    /// </summary>
    public string NameScalar { get; }

    /// <summary>
    /// Gets the name of the URI type scalar.
    /// </summary>
    public string ResolverKindEnum { get; }
    
    /// <summary>
    /// Gets the name of the GraphQL schema coordinate scalar.
    /// </summary>
    public string SchemaCoordinateScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL selection scalar.
    /// </summary>
    public string SelectionScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL selection set scalar.
    /// </summary>
    public string SelectionSetScalar { get; }

    /// <summary>
    /// Gets the name of the GraphQL type scalar.
    /// </summary>
    public string TypeScalar { get; }

    /// <summary>
    /// Gets the name of the URI type scalar.
    /// </summary>
    public string UriScalar { get; }

    /// <summary>
    /// Gets the name of the URI type scalar.
    /// </summary>
    public string OperationDefinitionScalar { get; }

    /// <summary>
    /// Gets the name of the declare directive.
    /// </summary>
    public string DeclareDirective { get; }

    /// <summary>
    /// Gets the name of the fusion directive.
    /// </summary>
    public string FusionDirective { get; }

    /// <summary>
    /// Gets the name of the is directive.
    /// </summary>
    public string IsDirective { get; }

    /// <summary>
    /// Gets the name of the node directive.
    /// </summary>
    public string NodeDirective { get; }

    /// <summary>
    /// Gets the name of the private directive.
    /// </summary>
    public string PrivateDirective { get; }

    /// <summary>
    /// Gets the name of the remove directive.
    /// </summary>
    public string RemoveDirective { get; }

    /// <summary>
    /// Gets the name of the rename directive.
    /// </summary>
    public string RenameDirective { get; }

    /// <summary>
    /// Gets the name of the require directive.
    /// </summary>
    public string RequireDirective { get; }

    /// <summary>
    /// Gets the name of the resolver directive.
    /// </summary>
    public string ResolverDirective { get; }

    /// <summary>
    /// Gets the name of the resolve directive.
    /// </summary>
    public string ResolveDirective { get; }

    /// <summary>
    /// Gets the name of the source directive.
    /// </summary>
    public string SourceDirective { get; }

    /// <summary>
    /// Gets the name of the transport directive.
    /// </summary>
    public string TransportDirective { get; }

    /// <summary>
    /// Gets the name of the variable directive.
    /// </summary>
    public string VariableDirective { get; }

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
        => prefix is not null
            ? new FusionTypeNames(prefix, prefixSelf)
            : new FusionTypeNames();

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
}