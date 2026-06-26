using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed class SelectionSetPartitioner(FusionSchemaDefinition schema)
{
    public SelectionSetPartitionerResult Partition(
        SelectionSetPartitionerInput input)
    {
        var context = new Context
        {
            SchemaName = input.SchemaName,
            RootPath = input.SelectionSet.Path,
            SelectionSetIndex = input.SelectionSetIndex,
            PruneUnprovidedAbstractBranches = input.PruneUnprovidedAbstractBranches
        };

        var (resolvable, _) =
            RewriteSelectionSet(
                context,
                input.SelectionSet.Type,
                input.SelectionSet.Node,
                input.ProvidedSelectionSet,
                // An incoming provided set is the complete data an event stream message already
                // delivers, so fields it does not cover are spilled. A @provides scope, in
                // contrast, is partial: it is layered on native ownership and is introduced
                // further down during recursion, never here at the entry point.
                input.ProvidedSelectionSet is not null
                    ? ProvidedCoverage.Complete
                    : ProvidedCoverage.Partial);

        return new SelectionSetPartitionerResult(
            resolvable,
            context.Unresolvable,
            context.FieldsWithRequirements,
            context.SelectionSetIndex);
    }

    private static ExecutionNodeCondition[]? ExtractDirectiveConditions(
        IReadOnlyList<DirectiveNode> directives)
    {
        List<ExecutionNodeCondition>? conditions = null;

        foreach (var directive in directives)
        {
            var passingValue = directive.Name.Value switch
            {
                "skip" => false,
                "include" => true,
                _ => (bool?)null
            };

            if (passingValue.HasValue
                && directive.Arguments.Count > 0
                && directive.Arguments[0].Value is VariableNode variable)
            {
                conditions ??= [];
                conditions.Add(new ExecutionNodeCondition
                {
                    VariableName = variable.Name.Value,
                    PassingValue = passingValue.Value,
                    Directive = directive
                });
            }
        }

        return conditions?.ToArray();
    }

    private (SelectionSetNode?, SelectionSetNode?) RewriteSelectionSet(
        Context context,
        ITypeDefinition type,
        SelectionSetNode selectionSetNode,
        SelectionSetNode? providedSelectionSetNode,
        ProvidedCoverage coverage)
    {
        var complexType = type as FusionComplexTypeDefinition;
        List<ISelectionNode>? resolvableSelections = null;
        List<ISelectionNode>? unresolvableSelections = null;

        providedSelectionSetNode = GetProvidedSelectionSet(type, schema, providedSelectionSetNode);

        context.Nodes.Push(selectionSetNode);

        for (var i = 0; i < selectionSetNode.Selections.Count; i++)
        {
            switch (selectionSetNode.Selections[i])
            {
                case FieldNode fieldNode:
                {
                    // The __typename field is available on all subgraphs, so we always treat it as resolvable.
                    // We need to check it like this to also handle the union { __typename } case.
                    if (fieldNode.Name.Value.Equals(IntrospectionFieldNames.TypeName))
                    {
                        CompleteSelection(fieldNode, fieldNode, null, i);
                    }
                    else
                    {
                        if (type == schema.QueryType)
                        {
                            var field = complexType!.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                            if (field.IsIntrospectionField)
                            {
                                CompleteSelection(fieldNode, null, null, i);
                                continue;
                            }
                        }

                        var fieldConditions = ExtractDirectiveConditions(fieldNode.Directives);
                        var savedCount = context.PushConditions(fieldConditions);

                        var (resolvable, unresolvable) =
                            RewriteFieldNode(
                                context,
                                complexType!,
                                fieldNode,
                                GetProvidedField(fieldNode, providedSelectionSetNode),
                                coverage);

                        context.PopConditions(savedCount);

                        CompleteSelection(fieldNode, resolvable, unresolvable, i);
                    }
                    break;
                }

                case InlineFragmentNode inlineFragmentNode:
                {
                    var fragmentConditions = ExtractDirectiveConditions(inlineFragmentNode.Directives);
                    var savedCount = context.PushConditions(fragmentConditions);

                    {
                        var (resolvable, unresolvable) =
                            RewriteFragmentNode(
                                context,
                                type,
                                inlineFragmentNode,
                                providedSelectionSetNode,
                                coverage);

                        CompleteSelection(inlineFragmentNode, resolvable, unresolvable, i);
                    }

                    context.PopConditions(savedCount);
                    break;
                }
            }
        }

        context.Nodes.Pop();

        var isAbstractType = type.NamedType().IsAbstractType();

        if (resolvableSelections is null && unresolvableSelections is null && !isAbstractType)
        {
            return (selectionSetNode, null);
        }

        if (unresolvableSelections is not null)
        {
            // When we have unresolvable selections on an abstract type, check if inner
            // recursive calls already pushed type-specific entries (from inline fragments)
            // to the unresolvable stack. If so, merge them into this entry to avoid
            // creating duplicate lookups for the same downstream schema.
            if (isAbstractType && !context.Unresolvable.IsEmpty)
            {
                var currentPath = context.BuildPath();
                var currentConditions = context.SnapshotConditions();
                MergeChildUnresolvableEntries(context, currentPath, currentConditions, unresolvableSelections);
            }

            var unresolvableSelectionSet = new SelectionSetNode(unresolvableSelections);
            context.Register(selectionSetNode, unresolvableSelectionSet);

            var selectionSet = new SelectionSet(
                context.GetId(selectionSetNode),
                unresolvableSelectionSet,
                type,
                context.BuildPath());
            context.Unresolvable = context.Unresolvable.Push(
                new ConditionedSelectionSet(selectionSet, context.SnapshotConditions()));
            unresolvableSelections = null;
        }

        resolvableSelections ??= [.. selectionSetNode.Selections];

        if (isAbstractType && !resolvableSelections.Any(IsTypeNameSelection))
        {
            resolvableSelections = [
                new FieldNode(IntrospectionFieldNames.TypeName),
                ..resolvableSelections];
        }

        var result =
        (
            Resolvable:
                selectionSetNode.WithSelections(resolvableSelections),
            Unresolvable: unresolvableSelections is not null
                ? selectionSetNode.WithSelections(unresolvableSelections)
                : null
        );

        context.Register(selectionSetNode, result.Resolvable);
        if (result.Unresolvable is not null)
        {
            context.Register(selectionSetNode, result.Unresolvable);
        }

        return result;

        void CompleteSelection<T>(T original, T? resolvable, T? unresolvable, int index) where T : ISelectionNode
        {
            if (resolvableSelections is null
                && (unresolvable is not null || !ReferenceEquals(resolvable, original)))
            {
                resolvableSelections ??= [];

                for (var j = 0; j < index; j++)
                {
                    resolvableSelections.Add(selectionSetNode.Selections[j]);
                }
            }

            if (unresolvable is not null)
            {
                unresolvableSelections ??= [];
                unresolvableSelections.Add(unresolvable);
            }

            if (resolvable is null)
            {
                return;
            }

            resolvableSelections?.Add(resolvable);
        }

        static FieldNode? GetProvidedField(FieldNode fieldNode, SelectionSetNode? providedSelectionSetNode)
        {
            return providedSelectionSetNode?.Selections
                .OfType<FieldNode>()
                .FirstOrDefault(t => t.Name.Value.Equals(fieldNode.Name.Value));
        }

        static SelectionSetNode? GetProvidedSelectionSet(
            ITypeDefinition type,
            FusionSchemaDefinition schema,
            SelectionSetNode? providedSelectionSetNode)
        {
            if (providedSelectionSetNode is null)
            {
                return null;
            }

            List<ISelectionNode>? flattened = null;
            var hasFragment = false;

            for (var i = 0; i < providedSelectionSetNode.Selections.Count; i++)
            {
                var selection = providedSelectionSetNode.Selections[i];

                if (selection is InlineFragmentNode fragment)
                {
                    hasFragment = true;

                    if (fragment.TypeCondition is null)
                    {
                        flattened ??= CopyUpTo(providedSelectionSetNode.Selections, i);
                        flattened.AddRange(fragment.SelectionSet.Selections);
                        continue;
                    }

                    if (!schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var fragmentType))
                    {
                        flattened ??= CopyUpTo(providedSelectionSetNode.Selections, i);
                        continue;
                    }

                    if (fragmentType.IsAssignableFrom(type))
                    {
                        flattened ??= CopyUpTo(providedSelectionSetNode.Selections, i);
                        flattened.AddRange(fragment.SelectionSet.Selections);
                    }
                    else if (type.IsAssignableFrom(fragmentType))
                    {
                        flattened ??= CopyUpTo(providedSelectionSetNode.Selections, i);
                        flattened.Add(fragment);
                    }
                    else
                    {
                        flattened ??= CopyUpTo(providedSelectionSetNode.Selections, i);
                    }
                }
                else
                {
                    flattened?.Add(selection);
                }
            }

            if (!hasFragment)
            {
                return providedSelectionSetNode;
            }

            return flattened is null
                ? providedSelectionSetNode
                : new SelectionSetNode(flattened);
        }

        static List<ISelectionNode> CopyUpTo(IReadOnlyList<ISelectionNode> selections, int exclusiveEnd)
        {
            var copy = new List<ISelectionNode>(selections.Count);
            for (var j = 0; j < exclusiveEnd; j++)
            {
                copy.Add(selections[j]);
            }
            return copy;
        }

        static bool IsTypeNameSelection(ISelectionNode selection)
        {
            if (selection is FieldNode field)
            {
                return field.Name.Value.Equals(IntrospectionFieldNames.TypeName)
                    && field.Alias is null;
            }

            return false;
        }
    }

    /// <summary>
    /// Merges child unresolvable entries from inline fragments back into the parent's
    /// unresolvable selections. This prevents duplicate lookups when both interface-level
    /// fields and type-refinement fields target the same downstream schema.
    /// </summary>
    private static void MergeChildUnresolvableEntries(
        Context context,
        SelectionPath currentPath,
        ExecutionNodeCondition[] currentConditions,
        List<ISelectionNode> unresolvableSelections)
    {
        List<ConditionedSelectionSet>? kept = null;
        var anyMerged = false;

        foreach (var entry in context.Unresolvable)
        {
            var entryPath = entry.SelectionSet.Path;

            // A child entry has exactly one more segment than the current path,
            // and that extra segment is an InlineFragment. The child's conditions must
            // start with the parent's conditions (prefix check), because the Unresolvable
            // stack is shared across sibling fragments and may contain entries from
            // unconditional siblings that must not inherit the parent's conditions.
            if (entryPath.Length == currentPath.Length + 1
                && entryPath[entryPath.Length - 1].Kind == SelectionPathSegmentKind.InlineFragment
                && currentPath.IsParentOfOrSame(entryPath)
                && IsConditionPrefix(currentConditions, entry.Conditions))
            {
                var typeName = entryPath[entryPath.Length - 1].Name;
                var selectionSet = WrapInExtraConditions(
                    context,
                    entry.SelectionSet.Node,
                    entry.Conditions,
                    currentConditions.Length);

                unresolvableSelections.Add(new InlineFragmentNode(
                    null,
                    new NamedTypeNode(typeName),
                    [],
                    selectionSet));
                anyMerged = true;
            }
            else
            {
                kept ??= [];
                kept.Add(entry);
            }
        }

        if (anyMerged)
        {
            // Rebuild the stack in reverse so the original ordering is preserved,
            // since ImmutableStack enumeration is LIFO.
            var stack = ImmutableStack<ConditionedSelectionSet>.Empty;
            if (kept is not null)
            {
                for (var i = kept.Count - 1; i >= 0; i--)
                {
                    stack = stack.Push(kept[i]);
                }
            }

            context.Unresolvable = stack;
        }
    }

    /// <summary>
    /// Checks whether <paramref name="prefix"/> is a prefix of <paramref name="conditions"/>.
    /// Conditions accumulate as the partitioner recurses deeper, so a child entry's conditions
    /// always start with the parent's conditions followed by any additional ones. However,
    /// the Unresolvable stack is shared across sibling fragments, so entries from unconditional
    /// siblings may have fewer conditions than the current parent scope.
    /// </summary>
    private static bool IsConditionPrefix(
        ExecutionNodeCondition[] prefix,
        ExecutionNodeCondition[] conditions)
    {
        if (conditions.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (!ReferenceEquals(prefix[i], conditions[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Wraps a selection set in inline fragments for each extra condition directive
    /// beyond the parent's conditions. This preserves the conditional semantics
    /// within the merged selection set rather than as node-level conditions.
    /// </summary>
    private static SelectionSetNode WrapInExtraConditions(
        Context context,
        SelectionSetNode selectionSet,
        ExecutionNodeCondition[] conditions,
        int startIndex)
    {
        for (var i = conditions.Length - 1; i >= startIndex; i--)
        {
            selectionSet = new SelectionSetNode([
                new InlineFragmentNode(
                    null,
                    null,
                    [conditions[i].Directive!],
                    selectionSet)
            ]);
            context.SelectionSetIndexBuilder.Register(selectionSet);
        }

        return selectionSet;
    }

    private (FieldNode?, FieldNode?) RewriteFieldNode(
        Context context,
        FusionComplexTypeDefinition complexType,
        FieldNode fieldNode,
        FieldNode? providedFieldNode,
        ProvidedCoverage coverage)
    {
        var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);
        field.Sources.TryGetMember(context.SchemaName, out var source);

        // With complete coverage (an event stream message shape), the provided set is all the
        // data we already have here, so a field is only resolvable when the set covers it.
        // Native ownership is ignored, which spills selections the message does not carry.
        // With partial coverage (a @provides scope), the provided set only adds fields, so an
        // uncovered field falls back to native ownership and a non-external source resolves it.
        var isResolvable = providedFieldNode is not null
            || (coverage is ProvidedCoverage.Partial && source is { IsExternal: false });

        if (!isResolvable)
        {
            return (null, fieldNode);
        }

        if (source?.SourceTypeName is { } narrowedTypeName
            && field.Type.NamedType().IsAbstractType())
        {
            if (!schema.Types.TryGetType(narrowedTypeName, out var narrowedType))
            {
                throw new InvalidOperationException(
                    $"The narrowed source type '{narrowedTypeName}' for field "
                    + $"'{complexType.Name}.{field.Name}' in source schema '{context.SchemaName}' "
                    + "does not exist in the composite schema.");
            }

            if (narrowedType is not FusionObjectTypeDefinition narrowedObject)
            {
                throw new NotSupportedException(
                    $"Supertype narrowing of field '{complexType.Name}.{field.Name}' in source schema "
                    + $"'{context.SchemaName}' to the abstract type '{narrowedTypeName}' "
                    + "is not yet supported. Only narrowing to a concrete object type is currently supported.");
            }

            var coverageSelectionSet = GetCoverageSelectionSet(context.SelectionSetIndex, fieldNode.SelectionSet);
            var fieldNodeForSpill = coverageSelectionSet is not null
                && !ReferenceEquals(coverageSelectionSet, fieldNode.SelectionSet)
                    ? fieldNode.WithSelectionSet(coverageSelectionSet)
                    : fieldNode;

            if (!narrowedObject.Sources.TryGetMember(context.SchemaName, out var narrowedObjectSource))
            {
                throw new InvalidOperationException(
                    $"The narrowed source type '{narrowedTypeName}' for field "
                    + $"'{complexType.Name}.{field.Name}' is not declared in source schema "
                    + $"'{context.SchemaName}'.");
            }

            foreach (var typeCondition in GetTopLevelTypeConditions(coverageSelectionSet))
            {
                var conditionName = typeCondition.Name.Value;
                var covered =
                    string.Equals(conditionName, narrowedTypeName, StringComparison.Ordinal)
                    || narrowedObjectSource.Implements.Contains(conditionName)
                    || narrowedObjectSource.MemberOf.Contains(conditionName);

                if (!covered)
                {
                    return (null, fieldNodeForSpill);
                }
            }
        }

        if (providedFieldNode is null && source?.Requirements is not null)
        {
            context.FieldsWithRequirements =
                context.FieldsWithRequirements.Push(
                    new ConditionedFieldSelection(
                        new FieldSelection(
                            context.GetId((SelectionSetNode)context.Nodes.Peek()),
                            fieldNode,
                            field,
                            context.BuildPath()),
                        context.SnapshotConditions()));
            return (null, null);
        }

        var selectionSet = fieldNode.SelectionSet;

        if (selectionSet is not null)
        {
            context.Nodes.Push(fieldNode);

            var (resolvable, unresolvable) = RewriteSelectionSet(
                context,
                field.Type.AsTypeDefinition(),
                selectionSet,
                MergeProvidedSelectionSets(providedFieldNode?.SelectionSet, source?.Provides),
                // Complete coverage propagates only into a subtree the provided set covers (a
                // matched providedFieldNode). Uncovered fields revert to partial and fall back
                // to native resolvability. The set is the merged message and @provides shape, so
                // within an already complete event stream scope a field covered only by
                // @provides also stays complete (at worst an extra lookup).
                coverage is ProvidedCoverage.Complete && providedFieldNode is not null
                    ? ProvidedCoverage.Complete
                    : ProvidedCoverage.Partial);

            context.Nodes.Pop();

            if (!ReferenceEquals(resolvable, selectionSet))
            {
                return
                (
                    fieldNode.WithSelectionSet(resolvable),
                    unresolvable is null ? null : fieldNode.WithSelectionSet(unresolvable)
                );
            }
        }

        return (fieldNode, null);

        static SelectionSetNode? GetCoverageSelectionSet(
            ISelectionSetIndex index,
            SelectionSetNode? selectionSet)
        {
            if (selectionSet is null || !index.IsRegistered(selectionSet))
            {
                return selectionSet;
            }

            var selectionSetId = index.GetId(selectionSet);

            if (index.TryGetOriginalIdFromCloned(selectionSetId, out var originalId))
            {
                selectionSetId = originalId;
            }

            return index.TryGetSelectionSet(selectionSetId, out var original)
                ? original
                : selectionSet;
        }

        static IEnumerable<NamedTypeNode> GetTopLevelTypeConditions(SelectionSetNode? selectionSet)
        {
            if (selectionSet is null)
            {
                yield break;
            }

            foreach (var selection in selectionSet.Selections)
            {
                if (selection is InlineFragmentNode { TypeCondition: { } typeCondition })
                {
                    yield return typeCondition;
                }
                else if (selection is InlineFragmentNode conditionlessFragment)
                {
                    foreach (var nestedTypeCondition in GetTopLevelTypeConditions(
                        conditionlessFragment.SelectionSet))
                    {
                        yield return nestedTypeCondition;
                    }
                }
            }
        }
    }

    private static SelectionSetNode? MergeProvidedSelectionSets(
        SelectionSetNode? inherited,
        SelectionSetNode? fromSource)
    {
        if (inherited is null)
        {
            return fromSource;
        }

        if (fromSource is null)
        {
            return inherited;
        }

        var merged = new List<ISelectionNode>(inherited.Selections.Count + fromSource.Selections.Count);
        merged.AddRange(inherited.Selections);
        merged.AddRange(fromSource.Selections);
        return new SelectionSetNode(merged);
    }

    private (InlineFragmentNode?, InlineFragmentNode?) RewriteFragmentNode(
        Context context,
        ITypeDefinition type,
        InlineFragmentNode inlineFragmentNode,
        SelectionSetNode? providedFieldNode,
        ProvidedCoverage coverage)
    {
        // TODO: we need to implement proper type routing here later.
        var typeCondition = type;

        if (inlineFragmentNode.TypeCondition is not null)
        {
            typeCondition = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
        }

        if (context.PruneUnprovidedAbstractBranches
            && inlineFragmentNode.TypeCondition is not null
            && providedFieldNode is not null
            && !CanMessageShapeProvideType(typeCondition, providedFieldNode))
        {
            return (null, null);
        }

        if (!typeCondition.ExistsInSchema(context.SchemaName))
        {
            return (null, null);
        }

        context.Nodes.Push(inlineFragmentNode);

        var (resolvable, unresolvable) =
            RewriteSelectionSet(
                context,
                typeCondition,
                inlineFragmentNode.SelectionSet,
                providedFieldNode,
                coverage);

        context.Nodes.Pop();

        if (resolvable is null)
        {
            return (null, inlineFragmentNode);
        }

        return
        (
            inlineFragmentNode.WithSelectionSet(resolvable),
            unresolvable is null ? null : inlineFragmentNode.WithSelectionSet(unresolvable)
        );
    }

    private sealed class Context
    {
        public required string SchemaName { get; init; }

        public required SelectionPath RootPath { get; init; }

        public required ISelectionSetIndex SelectionSetIndex { get; set; } = null!;

        public bool PruneUnprovidedAbstractBranches { get; init; }

        [field: AllowNull, MaybeNull]
        public SelectionSetIndexBuilder SelectionSetIndexBuilder
        {
            get
            {
                if (field is null)
                {
                    field = SelectionSetIndex.ToBuilder();
                    SelectionSetIndex = field;
                }

                return field;
            }
        }

        public ImmutableStack<ConditionedSelectionSet> Unresolvable { get; set; } = [];

        public ImmutableStack<ConditionedFieldSelection> FieldsWithRequirements { get; set; } = [];

        public List<ISyntaxNode> Nodes { get; } = [];

        private List<ExecutionNodeCondition> ActiveConditions { get; } = [];

        public int PushConditions(ExecutionNodeCondition[]? conditions)
        {
            if (conditions is null || conditions.Length == 0)
            {
                return ActiveConditions.Count;
            }

            var savedCount = ActiveConditions.Count;
            ActiveConditions.AddRange(conditions);
            return savedCount;
        }

        public void PopConditions(int savedCount)
        {
            if (savedCount < ActiveConditions.Count)
            {
                ActiveConditions.RemoveRange(savedCount, ActiveConditions.Count - savedCount);
            }
        }

        public ExecutionNodeCondition[] SnapshotConditions()
        {
            return ActiveConditions.Count == 0
                ? []
                : ActiveConditions.ToArray();
        }

        public SelectionPath BuildPath()
        {
            var builder = SelectionPath.CreateBuilder(RootPath);

            foreach (var node in Nodes)
            {
                switch (node)
                {
                    case FieldNode fieldNode:
                        builder.AppendField(fieldNode.Alias?.Value ?? fieldNode.Name.Value);
                        break;

                    case InlineFragmentNode { TypeCondition: not null } inlineFragmentNode:
                        builder.AppendFragment(inlineFragmentNode.TypeCondition.Name.Value);
                        break;
                }
            }

            return builder.Build();
        }

        public uint GetId(SelectionSetNode selectionSetNode)
            => SelectionSetIndex.GetId(selectionSetNode);

        public void Register(SelectionSetNode original, SelectionSetNode branch)
        {
            if (ReferenceEquals(original, branch))
            {
                return;
            }

            if (SelectionSetIndex.IsRegistered(branch))
            {
                return;
            }

            SelectionSetIndexBuilder.Register(original, branch);
        }
    }

    private bool CanMessageShapeProvideType(
        ITypeDefinition typeCondition,
        SelectionSetNode providedSelectionSet)
    {
        var hasTypedShape = false;
        return HasApplicableType(typeCondition, providedSelectionSet, ref hasTypedShape) || !hasTypedShape;
    }

    private bool HasApplicableType(
        ITypeDefinition typeCondition,
        SelectionSetNode providedSelectionSet,
        ref bool hasTypedShape)
    {
        foreach (var selection in providedSelectionSet.Selections)
        {
            if (selection is not InlineFragmentNode fragment)
            {
                continue;
            }

            if (fragment.TypeCondition is null)
            {
                if (HasApplicableType(typeCondition, fragment.SelectionSet, ref hasTypedShape))
                {
                    return true;
                }

                continue;
            }

            if (!schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var providedType))
            {
                continue;
            }

            hasTypedShape = true;

            if (typeCondition.IsAssignableFrom(providedType)
                || providedType.IsAssignableFrom(typeCondition))
            {
                return true;
            }
        }

        return false;
    }
}
