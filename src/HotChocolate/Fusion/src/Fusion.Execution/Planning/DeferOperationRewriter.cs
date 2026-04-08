using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Splits an operation with <c>@defer</c> directives into a main operation
/// (without deferred inline fragments) and one or more deferred fragment operations.
/// </summary>
internal sealed class DeferOperationRewriter
{
    private int _nextDeferId;

    /// <summary>
    /// Fast check whether the operation contains any <c>@defer</c> directives.
    /// Used to avoid the full split for non-deferred operations (the common case).
    /// </summary>
    public static bool HasDeferDirective(OperationDefinitionNode operation)
    {
        return HasDeferInSelectionSet(operation.SelectionSet);

        static bool HasDeferInSelectionSet(SelectionSetNode selectionSet)
        {
            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                var selection = selectionSet.Selections[i];

                if (selection is InlineFragmentNode inlineFragment)
                {
                    if (HasDeferDirective(inlineFragment))
                    {
                        return true;
                    }

                    if (HasDeferInSelectionSet(inlineFragment.SelectionSet))
                    {
                        return true;
                    }
                }
                else if (selection is FragmentSpreadNode fragmentSpread)
                {
                    if (HasDeferDirectiveOnSpread(fragmentSpread))
                    {
                        return true;
                    }
                }
                else if (selection is FieldNode { SelectionSet: not null } field)
                {
                    if (HasDeferInSelectionSet(field.SelectionSet))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool HasDeferDirectiveOnSpread(FragmentSpreadNode node)
        {
            for (var i = 0; i < node.Directives.Count; i++)
            {
                if (node.Directives[i].Name.Value.Equals(
                    DirectiveNames.Defer.Name,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Splits the given operation at <c>@defer</c> boundaries.
    /// </summary>
    /// <param name="operation">The operation definition that may contain @defer directives.</param>
    /// <returns>
    /// A result containing the stripped main operation and a list of deferred fragment descriptors.
    /// </returns>
    public DeferSplitResult Split(OperationDefinitionNode operation)
    {
        _nextDeferId = 0;
        var deferredFragments = ImmutableArray.CreateBuilder<DeferredFragmentDescriptor>();

        var mainSelectionSet = StripDeferFragments(
            operation.SelectionSet,
            [],
            deferredFragments,
            operation,
            parentDeferFragment: null);

        var mainOperation = operation.WithSelectionSet(mainSelectionSet);

        return new DeferSplitResult(mainOperation, deferredFragments.ToImmutable());
    }

    /// <summary>
    /// Recursively walks selection sets, removes @defer inline fragments,
    /// and creates deferred operations for each.
    /// </summary>
    private SelectionSetNode StripDeferFragments(
        SelectionSetNode selectionSet,
        ImmutableArray<FieldPathSegment> parentPath,
        ImmutableArray<DeferredFragmentDescriptor>.Builder deferredFragments,
        OperationDefinitionNode rootOperation,
        DeferredFragmentDescriptor? parentDeferFragment)
    {
        var selections = new List<ISelectionNode>(selectionSet.Selections.Count);
        var modified = false;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection is InlineFragmentNode inlineFragment && HasDeferDirective(inlineFragment))
            {
                var (label, ifVariable) = ExtractDeferArgs(inlineFragment);

                // If @defer(if: false) literal, keep the fragment inline (don't defer)
                if (ShouldSkipDefer(inlineFragment))
                {
                    var stripped = StripDeferDirective(inlineFragment);
                    var newInnerSelectionSet = StripDeferFragments(
                        stripped.SelectionSet,
                        parentPath,
                        deferredFragments,
                        rootOperation,
                        parentDeferFragment);

                    if (!ReferenceEquals(newInnerSelectionSet, stripped.SelectionSet))
                    {
                        stripped = stripped.WithSelectionSet(newInnerSelectionSet);
                    }

                    selections.Add(stripped);
                    modified = true;
                    continue;
                }

                var deferId = _nextDeferId++;

                // Create the descriptor first so nested defers can reference it as parent.
                // We'll set the operation after stripping nested defers.
                var descriptor = new DeferredFragmentDescriptor(
                    deferId,
                    label,
                    ifVariable,
                    parentPath,
                    null!, // placeholder — set below
                    parentDeferFragment);

                deferredFragments.Add(descriptor);

                // Recurse to strip nested @defer from the deferred fragment
                var strippedInnerSelectionSet = StripDeferFragments(
                    inlineFragment.SelectionSet,
                    parentPath,
                    deferredFragments,
                    rootOperation,
                    parentDeferFragment: descriptor);

                // Build the deferred operation using the STRIPPED inner selections
                var strippedFragment = StripDeferDirective(inlineFragment);
                if (!ReferenceEquals(strippedInnerSelectionSet, inlineFragment.SelectionSet))
                {
                    strippedFragment = strippedFragment.WithSelectionSet(strippedInnerSelectionSet);
                }

                var deferredOperation = BuildDeferredOperation(
                    rootOperation,
                    parentPath,
                    strippedFragment);

                // Now set the real operation on the descriptor
                descriptor.Operation = deferredOperation;

                // If conditional (@defer(if: $variable)), also keep the fragment
                // inline in the main operation but with @skip(if: $variable).
                // When defer is active (variable=true), @skip removes the fields
                // from the main response. When defer is inactive (variable=false),
                // @skip doesn't apply and the fields are fetched inline.
                if (ifVariable is not null)
                {
                    var skipDirective = new DirectiveNode(
                        null,
                        new NameNode("skip"),
                        [
                            new ArgumentNode(
                                null,
                                new NameNode("if"),
                                new VariableNode(new NameNode(ifVariable)))
                        ]);

                    selections.Add(
                        strippedFragment.WithDirectives(
                            [..strippedFragment.Directives, skipDirective]));
                }

                modified = true;
                continue;
            }

            if (selection is FieldNode fieldNode && fieldNode.SelectionSet is not null)
            {
                // Recurse into child selection sets to find nested @defer
                var childPath = parentPath.Add(
                    new FieldPathSegment(fieldNode.Name.Value, fieldNode.Alias?.Value));

                var newChildSelectionSet = StripDeferFragments(
                    fieldNode.SelectionSet,
                    childPath,
                    deferredFragments,
                    rootOperation,
                    parentDeferFragment);

                if (!ReferenceEquals(newChildSelectionSet, fieldNode.SelectionSet))
                {
                    fieldNode = fieldNode.WithSelectionSet(newChildSelectionSet);
                    modified = true;
                }

                selections.Add(fieldNode);
            }
            else if (selection is InlineFragmentNode nonDeferInlineFragment
                && nonDeferInlineFragment.SelectionSet is not null)
            {
                // Recurse into non-defer inline fragments
                var newInnerSelectionSet = StripDeferFragments(
                    nonDeferInlineFragment.SelectionSet,
                    parentPath,
                    deferredFragments,
                    rootOperation,
                    parentDeferFragment);

                if (!ReferenceEquals(newInnerSelectionSet, nonDeferInlineFragment.SelectionSet))
                {
                    nonDeferInlineFragment = nonDeferInlineFragment.WithSelectionSet(newInnerSelectionSet);
                    modified = true;
                }

                selections.Add(nonDeferInlineFragment);
            }
            else
            {
                selections.Add(selection);
            }
        }

        if (!modified)
        {
            return selectionSet;
        }

        // If removing deferred fragments left the selection set empty, add __typename
        if (selections.Count == 0)
        {
            selections.Add(new FieldNode("__typename"));
        }

        return new SelectionSetNode(selections);
    }

    /// <summary>
    /// Builds a standalone operation for a deferred fragment by wrapping
    /// the deferred selections in the parent field path.
    /// </summary>
    private static OperationDefinitionNode BuildDeferredOperation(
        OperationDefinitionNode rootOperation,
        ImmutableArray<FieldPathSegment> parentPath,
        InlineFragmentNode deferredFragment)
    {
        // Start with the deferred fragment's selection set
        SelectionSetNode currentSelectionSet = deferredFragment.SelectionSet;

        // If the inline fragment has a type condition, wrap selections in the type condition
        if (deferredFragment.TypeCondition is not null)
        {
            currentSelectionSet = new SelectionSetNode(
                new ISelectionNode[]
                {
                    new InlineFragmentNode(
                        null,
                        deferredFragment.TypeCondition,
                        deferredFragment.Directives.Count > 0
                            ? deferredFragment.Directives
                            : [],
                        currentSelectionSet)
                });
        }

        // Walk the path from innermost to outermost, wrapping in parent fields.
        // We need to reconstruct the field nodes with their arguments to reach the deferred data.
        // For this, we look up the original fields in the root operation's AST.
        var pathFields = ResolvePathFields(rootOperation.SelectionSet, parentPath);

        for (var i = pathFields.Length - 1; i >= 0; i--)
        {
            var pathField = pathFields[i];
            var wrappingField = new FieldNode(
                null,
                pathField.Name,
                pathField.Alias,
                pathField.Directives,
                pathField.Arguments,
                currentSelectionSet);
            currentSelectionSet = new SelectionSetNode(new ISelectionNode[] { wrappingField });
        }

        return rootOperation
            .WithOperation(OperationType.Query)
            .WithDirectives([])
            .WithSelectionSet(currentSelectionSet);
    }

    /// <summary>
    /// Resolves the FieldNode at each segment of the path by walking the operation's AST.
    /// </summary>
    private static ImmutableArray<FieldNode> ResolvePathFields(
        SelectionSetNode selectionSet,
        ImmutableArray<FieldPathSegment> path)
    {
        var result = ImmutableArray.CreateBuilder<FieldNode>(path.Length);
        var current = selectionSet;

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];
            var field = FindField(current, segment);

            if (field is null)
            {
                break;
            }

            result.Add(field);

            if (field.SelectionSet is not null)
            {
                current = field.SelectionSet;
            }
        }

        return result.ToImmutable();
    }

    private static FieldNode? FindField(SelectionSetNode selectionSet, FieldPathSegment segment)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            if (selectionSet.Selections[i] is FieldNode field)
            {
                var responseName = field.Alias?.Value ?? field.Name.Value;

                if (responseName.Equals(segment.ResponseName, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            // Also look inside non-defer inline fragments
            if (selectionSet.Selections[i] is InlineFragmentNode inlineFragment
                && !HasDeferDirective(inlineFragment))
            {
                var found = FindField(inlineFragment.SelectionSet, segment);

                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static InlineFragmentNode StripDeferDirective(InlineFragmentNode node)
    {
        var directives = new List<DirectiveNode>(node.Directives.Count);

        for (var i = 0; i < node.Directives.Count; i++)
        {
            if (!node.Directives[i].Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                directives.Add(node.Directives[i]);
            }
        }

        return node.WithDirectives(directives);
    }

    private static bool HasDeferDirective(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            if (node.Directives[i].Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldSkipDefer(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var arg = directive.Arguments[j];

                if (arg.Name.Value.Equals("if", StringComparison.Ordinal)
                    && arg.Value is BooleanValueNode { Value: false })
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (string? Label, string? IfVariable) ExtractDeferArgs(InlineFragmentNode node)
    {
        string? label = null;
        string? ifVariable = null;

        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var arg = directive.Arguments[j];

                if (arg.Name.Value.Equals("label", StringComparison.Ordinal)
                    && arg.Value is StringValueNode labelValue)
                {
                    label = labelValue.Value;
                }
                else if (arg.Name.Value.Equals("if", StringComparison.Ordinal)
                    && arg.Value is VariableNode variableNode)
                {
                    ifVariable = variableNode.Name.Value;
                }
            }

            break;
        }

        return (label, ifVariable);
    }
}

/// <summary>
/// The result of splitting an operation at @defer boundaries.
/// </summary>
internal readonly record struct DeferSplitResult(
    OperationDefinitionNode MainOperation,
    ImmutableArray<DeferredFragmentDescriptor> DeferredFragments);

/// <summary>
/// Describes a single @defer fragment extracted from an operation.
/// </summary>
internal sealed class DeferredFragmentDescriptor
{
    public DeferredFragmentDescriptor(
        int deferId,
        string? label,
        string? ifVariable,
        ImmutableArray<FieldPathSegment> path,
        OperationDefinitionNode operation,
        DeferredFragmentDescriptor? parent)
    {
        DeferId = deferId;
        Label = label;
        IfVariable = ifVariable;
        Path = path;
        Operation = operation;
        Parent = parent;
    }

    public int DeferId { get; }
    public string? Label { get; }
    public string? IfVariable { get; }
    public ImmutableArray<FieldPathSegment> Path { get; }
    public OperationDefinitionNode Operation { get; internal set; }
    public DeferredFragmentDescriptor? Parent { get; }
}

/// <summary>
/// A segment of a field path, identifying a field by its name and optional alias.
/// </summary>
internal readonly record struct FieldPathSegment(string FieldName, string? Alias)
{
    public string ResponseName => Alias ?? FieldName;
}
