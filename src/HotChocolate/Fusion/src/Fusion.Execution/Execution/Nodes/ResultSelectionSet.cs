using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// A pre-computed lookup structure that mirrors a <see cref="SelectionSetNode"/>
/// for tracking which fields in the result tree belong to an execution node.
/// </summary>
internal sealed class ResultSelectionSet
{
    private const int SmallThreshold = 8;
    private const string ResponseNameDirective = "fusion__responseName";

    private readonly ResultSelection[] _selections;
    private readonly ResultFragment[] _fragments;
    private readonly string[] _allResponseNames;
    private readonly SourceResponseNameMapping[]? _sourceResponseNameMappings;
    private readonly Dictionary<string, ResultSelectionSet?>? _childLookup;

    private ResultSelectionSet(
        ResultSelection[] selections,
        ResultFragment[] fragments,
        string[] allResponseNames,
        SourceResponseNameMapping[]? sourceResponseNameMappings)
    {
        _selections = selections;
        _fragments = fragments;
        _allResponseNames = allResponseNames;
        _sourceResponseNameMappings = sourceResponseNameMappings;

        HasSourceResponseNameMappings =
            sourceResponseNameMappings is not null
            || fragments.Any(static fragment => fragment.Body.HasSourceResponseNameMappings);

        if (selections.Length >= SmallThreshold)
        {
            var lookup = new Dictionary<string, ResultSelectionSet?>(
                selections.Length,
                StringComparer.Ordinal);

            for (var i = 0; i < selections.Length; i++)
            {
                lookup[selections[i].ResponseName] = selections[i].Child;
            }

            _childLookup = lookup;
        }
    }

    /// <summary>
    /// The pre-computed union of ALL response names at this level,
    /// including those inside inline fragments. Used by error pocketing
    /// and error result building where over-approximation is safe.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => _allResponseNames;

    public bool HasSourceResponseNameMappings { get; }

    /// <summary>
    /// Gets a value indicating whether the objects described by this selection set are opaque
    /// interface values produced by an <c>@interfaceObject</c> stand-in. Such values initialize
    /// interface-typed and only recover their concrete identity through a covering lookup.
    /// </summary>
    public bool ProducesOpaqueElements { get; private set; }

    private void MarkProducesOpaqueElements() => ProducesOpaqueElements = true;

    /// <summary>
    /// Gets a value indicating whether direct child lookups on this selection set
    /// are served by a dictionary rather than a linear scan. Exposed for diagnostics
    /// and benchmarks that assert the lookup strategy per selection set shape.
    /// </summary>
    internal bool UsesDictionaryLookup => _childLookup is not null;

    /// <summary>
    /// Gets the child selection set for a given response name (type-unaware).
    /// Searches direct selections first, then fragments (first match wins).
    /// Used at the <c>BuildResult</c> level where the runtime type isn't resolved yet.
    /// </summary>
    public ResultSelectionSet? TryGetChild(string responseName)
    {
        if (_childLookup is null)
        {
            var selections = _selections;

            for (var i = 0; i < selections.Length; i++)
            {
                if (string.Equals(
                    selections[i].ResponseName,
                    responseName,
                    StringComparison.Ordinal))
                {
                    return selections[i].Child;
                }
            }
        }
        else if (_childLookup.TryGetValue(responseName, out var child))
        {
            return child;
        }

        var fragments = _fragments;

        if (fragments.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < fragments.Length; i++)
        {
            if (fragments[i].Body.TryGetChild(responseName) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the child selection set for a given response name, filtered by type condition.
    /// Searches direct selections first, then only fragments whose type condition
    /// is <c>null</c> or is assignable from <paramref name="objectType"/>.
    /// Used in <c>TryCompleteObjectValue</c> where the runtime type is known.
    /// </summary>
    public ResultSelectionSet? TryGetChild(string responseName, IComplexTypeDefinition objectType)
    {
        if (_childLookup is null)
        {
            var selections = _selections;

            for (var i = 0; i < selections.Length; i++)
            {
                if (string.Equals(
                    selections[i].ResponseName,
                    responseName,
                    StringComparison.Ordinal))
                {
                    return selections[i].Child;
                }
            }
        }
        else if (_childLookup.TryGetValue(responseName, out var child))
        {
            return child;
        }

        var fragments = _fragments;

        if (fragments.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < fragments.Length; i++)
        {
            ref readonly var fragment = ref fragments[i];

            if (fragment.TypeCondition?.IsAssignableFrom(objectType) == false)
            {
                continue;
            }

            var result = fragment.Body.TryGetChild(responseName, objectType);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    public bool TryMapSourceResponseName(
        SourceResultProperty property,
        out SourceResponseNameMapping mapping)
    {
        var sourceResponseNameMappings = _sourceResponseNameMappings;

        if (sourceResponseNameMappings is not null)
        {
            for (var i = 0; i < sourceResponseNameMappings.Length; i++)
            {
                if (property.NameEquals(sourceResponseNameMappings[i].SourceResponseName))
                {
                    mapping = sourceResponseNameMappings[i];
                    return true;
                }
            }
        }

        var fragments = _fragments;

        if (fragments.Length == 0)
        {
            mapping = default;
            return false;
        }

        for (var i = 0; i < fragments.Length; i++)
        {
            if (fragments[i].Body.TryMapSourceResponseName(property, out mapping))
            {
                return true;
            }
        }

        mapping = default;
        return false;
    }

    /// <summary>
    /// Reconstructs a <see cref="SelectionSetNode"/> from this selection set.
    /// </summary>
    public SelectionSetNode ToSelectionSetNode()
    {
        var selections = new List<ISelectionNode>();

        foreach (var selection in _selections)
        {
            SourceResponseNameMapping? mapping = null;

            if (_sourceResponseNameMappings is not null)
            {
                for (var i = 0; i < _sourceResponseNameMappings.Length; i++)
                {
                    if (_sourceResponseNameMappings[i].ResponseName.Equals(
                        selection.ResponseName,
                        StringComparison.Ordinal))
                    {
                        mapping = _sourceResponseNameMappings[i];
                        break;
                    }
                }
            }

            if (mapping is { } sourceMapping)
            {
                selections.Add(new FieldNode(
                    new NameNode(sourceMapping.FieldName),
                    new NameNode(sourceMapping.SourceResponseName),
                    [new DirectiveNode(
                        ResponseNameDirective,
                        [new ArgumentNode("name", sourceMapping.ResponseName)])],
                    [],
                    selectionSet: selection.Child?.ToSelectionSetNode()));
            }
            else
            {
                selections.Add(new FieldNode(
                    selection.ResponseName,
                    selectionSet: selection.Child?.ToSelectionSetNode()));
            }
        }

        foreach (var fragment in _fragments)
        {
            selections.Add(new InlineFragmentNode(
                null,
                fragment.TypeCondition is not null
                    ? new NamedTypeNode(fragment.TypeCondition.Name)
                    : null,
                [],
                fragment.Body.ToSelectionSetNode()));
        }

        return new SelectionSetNode(selections);
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this selection set.
    /// </summary>
    public string ToString(bool indented)
        => ToSelectionSetNode().ToString(indented);

    /// <inheritdoc />
    public override string ToString()
        => ToString(indented: false);

    /// <summary>
    /// Creates a <see cref="ResultSelectionSet"/> from a <see cref="SelectionSetNode"/>.
    /// </summary>
    /// <param name="selectionSet">The AST selection set to build from.</param>
    /// <param name="schema">
    /// Optional schema used to resolve inline fragment type conditions to
    /// <see cref="ITypeDefinition"/> instances. When <c>null</c>, type conditions are not resolved.
    /// </param>
    /// <param name="parentType">
    /// The named type that declares this selection set. When provided together with
    /// <paramref name="sourceSchemaName"/>, it is used to detect fields that return opaque
    /// <c>@interfaceObject</c> stand-in values so the corresponding child set can be marked.
    /// </param>
    /// <param name="sourceSchemaName">
    /// The source schema that resolves this selection set. When it stands in for an interface a
    /// field returns, that field's child set produces opaque interface values.
    /// </param>
    public static ResultSelectionSet Create(
        SelectionSetNode selectionSet,
        ISchemaDefinition? schema = null,
        ITypeDefinition? parentType = null,
        string? sourceSchemaName = null)
        => Create(selectionSet, schema, parentType, sourceSchemaName, sourceAliases: null);

    public static ResultSelectionSet Create(
        SelectionSetNode selectionSet,
        ISchemaDefinition? schema,
        ITypeDefinition? parentType,
        string? sourceSchemaName,
        IReadOnlyDictionary<FieldNode, string>? sourceAliases)
    {
        var directSelections = new List<ResultSelection>();
        var fragments = new List<ResultFragment>();
        var allResponseNames = new HashSet<string>(StringComparer.Ordinal);
        List<SourceResponseNameMapping>? sourceResponseNameMappings = null;

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var name = field.Alias?.Value ?? field.Name.Value;
                    allResponseNames.Add(name);

                    if (sourceAliases is not null
                        && sourceAliases.TryGetValue(field, out var sourceAlias))
                    {
                        sourceResponseNameMappings ??= [];
                        sourceResponseNameMappings.Add(
                            new SourceResponseNameMapping(
                                field.Name.Value,
                                sourceAlias,
                                name));
                    }

                    ResultSelectionSet? child = null;
                    if (field.SelectionSet is { } childSet)
                    {
                        var fieldType = ResolveFieldType(parentType, field.Name.Value);
                        child = Create(
                            childSet,
                            schema,
                            fieldType,
                            sourceSchemaName,
                            sourceAliases);

                        if (IsOpaqueStandIn(fieldType, sourceSchemaName))
                        {
                            child.MarkProducesOpaqueElements();
                        }
                    }

                    directSelections.Add(new ResultSelection(name, child));
                    break;

                case InlineFragmentNode inlineFragment:
                    ITypeDefinition? typeCondition = null;
                    if (inlineFragment.TypeCondition is not null)
                    {
                        schema?.Types.TryGetType(
                            inlineFragment.TypeCondition.Name.Value,
                            out typeCondition);
                    }

                    var body = Create(
                        inlineFragment.SelectionSet,
                        schema,
                        typeCondition ?? parentType,
                        sourceSchemaName,
                        sourceAliases);

                    fragments.Add(new ResultFragment(typeCondition, body));

                    // Add the fragment body's response names to the union.
                    foreach (var responseName in body.ResponseNames)
                    {
                        allResponseNames.Add(responseName);
                    }

                    break;
            }
        }

        var selectionsArray = directSelections.ToArray();
        var fragmentsArray = fragments.ToArray();
        var responseNamesArray = new string[allResponseNames.Count];
        allResponseNames.CopyTo(responseNamesArray);

        return new ResultSelectionSet(
            selectionsArray,
            fragmentsArray,
            responseNamesArray,
            sourceResponseNameMappings?.ToArray());
    }

    public static ResultSelectionSet CreateFromPlan(
        SelectionSetNode selectionSet,
        ISchemaDefinition? schema = null,
        ITypeDefinition? parentType = null,
        string? sourceSchemaName = null)
    {
        Dictionary<FieldNode, string>? sourceAliases = null;
        var normalized = NormalizePlanSelectionSet(selectionSet, ref sourceAliases);
        return Create(normalized, schema, parentType, sourceSchemaName, sourceAliases);
    }

    private static SelectionSetNode NormalizePlanSelectionSet(
        SelectionSetNode selectionSet,
        ref Dictionary<FieldNode, string>? sourceAliases)
    {
        var selections = new ISelectionNode[selectionSet.Selections.Count];

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            switch (selectionSet.Selections[i])
            {
                case FieldNode field:
                    var child = field.SelectionSet is null
                        ? null
                        : NormalizePlanSelectionSet(field.SelectionSet, ref sourceAliases);
                    var responseName = GetMappedResponseName(field);
                    var normalized = new FieldNode(
                        field.Location,
                        field.Name,
                        responseName is null
                            || responseName.Equals(field.Name.Value, StringComparison.Ordinal)
                                ? null
                                : new NameNode(responseName),
                        RemoveResponseNameDirective(field.Directives),
                        field.Arguments,
                        child);

                    if (field.Alias is not null && responseName is not null)
                    {
                        sourceAliases ??= new Dictionary<FieldNode, string>(
                            ReferenceEqualityComparer.Instance);
                        sourceAliases.Add(normalized, field.Alias.Value);
                    }

                    selections[i] = normalized;
                    break;

                case InlineFragmentNode inlineFragment:
                    selections[i] = inlineFragment.WithSelectionSet(
                        NormalizePlanSelectionSet(
                            inlineFragment.SelectionSet,
                            ref sourceAliases));
                    break;

                default:
                    selections[i] = selectionSet.Selections[i];
                    break;
            }
        }

        return new SelectionSetNode(selections);
    }

    private static string? GetMappedResponseName(FieldNode field)
    {
        foreach (var directive in field.Directives)
        {
            if (directive.Name.Value.Equals(ResponseNameDirective, StringComparison.Ordinal)
                && directive.Arguments is
                [
                    {
                        Name.Value: "name",
                        Value: StringValueNode responseName
                    }
                ])
            {
                return responseName.Value;
            }
        }

        return null;
    }

    private static IReadOnlyList<DirectiveNode> RemoveResponseNameDirective(
        IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        List<DirectiveNode>? filtered = null;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            if (directive.Name.Value.Equals(ResponseNameDirective, StringComparison.Ordinal))
            {
                filtered ??= [with(directives.Count - 1)];
                for (var j = 0; j < i; j++)
                {
                    filtered.Add(directives[j]);
                }
            }
            else
            {
                filtered?.Add(directive);
            }
        }

        return filtered ?? directives;
    }

    private static ITypeDefinition? ResolveFieldType(ITypeDefinition? parentType, string fieldName)
        => parentType is IComplexTypeDefinition complexType
            && complexType.Fields.TryGetField(fieldName, out var field)
            ? field.Type.NamedType()
            : null;

    private static bool IsOpaqueStandIn(ITypeDefinition? namedType, string? sourceSchemaName)
        => sourceSchemaName is not null
            && namedType is FusionInterfaceTypeDefinition interfaceType
            && interfaceType.Sources.TryGetMember(sourceSchemaName, out var source)
            && source.IsInterfaceObject;
}
