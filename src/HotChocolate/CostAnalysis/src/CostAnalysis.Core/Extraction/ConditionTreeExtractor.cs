using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Extracts condition trees from raw GraphQL language nodes and a
/// <see cref="CostSchemaIndex"/>, with no dependency on document
/// validation or execution types.
/// </summary>
internal static class ConditionTreeExtractor
{
    /// <summary>
    /// Extracts the condition tree of an operation's root boundary,
    /// resolving named fragment spreads from <paramref name="document"/>.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index to resolve possible types and fields against.
    /// </param>
    /// <param name="document">
    /// The document the operation belongs to, searched for named fragment
    /// definitions.
    /// </param>
    /// <param name="operation">
    /// The operation whose root selection set is extracted.
    /// </param>
    /// <param name="rootTypeName">
    /// The name of the operation's root type.
    /// </param>
    /// <param name="knownVariableValues">
    /// Boolean variable values known at extraction time, used to prune
    /// selections a known-inactive <c>@include</c>/<c>@skip</c> removes.
    /// A known-active edge is kept, never collapsed. <see langword="null"/>
    /// extracts with every variable unknown.
    /// </param>
    public static ConditionTree ExtractOperation(
        CostSchemaIndex schemaIndex,
        DocumentNode document,
        OperationDefinitionNode operation,
        string rootTypeName,
        IReadOnlyDictionary<string, bool>? knownVariableValues = null)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(rootTypeName);

        var rootCondition = new Condition(schemaIndex.GetPossibleTypeSet(rootTypeName), []);

        return ExtractBoundary(
            schemaIndex,
            IndexFragments(document),
            operation.SelectionSet.Selections,
            rootCondition,
            knownVariableValues);
    }

    /// <summary>
    /// Extracts the condition tree of one selection-set boundary: the
    /// operation's root, or a nested field group's merged selection set.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index to resolve possible types and fields against.
    /// </param>
    /// <param name="fragments">
    /// The document's named fragment definitions, indexed by name.
    /// </param>
    /// <param name="selections">
    /// The boundary's selections.
    /// </param>
    /// <param name="rootCondition">
    /// The boundary's starting condition: the field's return type's
    /// possible types (or the schema's root type, for an operation), paired
    /// with the Boolean condition inherited from the enclosing boundary.
    /// </param>
    /// <param name="knownVariableValues">
    /// Boolean variable values known at extraction time, used to prune
    /// selections a known-inactive <c>@include</c>/<c>@skip</c> removes.
    /// <see langword="null"/> extracts with every variable unknown.
    /// </param>
    public static ConditionTree ExtractBoundary(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IReadOnlyList<ISelectionNode> selections,
        Condition rootCondition,
        IReadOnlyDictionary<string, bool>? knownVariableValues = null)
    {
        ArgumentNullException.ThrowIfNull(schemaIndex);
        ArgumentNullException.ThrowIfNull(fragments);
        ArgumentNullException.ThrowIfNull(selections);

        var builder = new ConditionTreeBuilder(schemaIndex, fragments, knownVariableValues);
        return builder.Build(selections, rootCondition);
    }

    /// <summary>
    /// Indexes every named fragment definition in <paramref name="document"/>
    /// by name.
    /// </summary>
    public static IReadOnlyDictionary<string, FragmentDefinitionNode> IndexFragments(DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Dictionary<string, FragmentDefinitionNode>? fragments = null;

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragment)
            {
                (fragments ??= [])[fragment.Name.Value] = fragment;
            }
        }

        return fragments ?? s_emptyFragments;
    }

    private static readonly Dictionary<string, FragmentDefinitionNode> s_emptyFragments = [];
}
