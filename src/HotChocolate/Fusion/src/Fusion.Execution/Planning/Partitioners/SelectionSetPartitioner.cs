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
            SelectionSetIndex = input.SelectionSetIndex
        };

        var (resolvable, _) =
            RewriteSelectionSet(
                context,
                input.SelectionSet.Type,
                input.SelectionSet.Node,
                null);

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
        SelectionSetNode? providedSelectionSetNode)
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
                                GetProvidedField(fieldNode, providedSelectionSetNode));

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
                                providedSelectionSetNode);

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
            if (providedSelectionSetNode is not null)
            {
                return providedSelectionSetNode.Selections
                    .OfType<FieldNode>()
                    .FirstOrDefault(t => t.Name.Value.Equals(fieldNode.Name.Value));
            }

            return null;
        }

        static SelectionSetNode? GetProvidedSelectionSet(
            ITypeDefinition type,
            FusionSchemaDefinition schema,
            SelectionSetNode? providedSelectionSetNode)
        {
            // todo match correct inline fragment
            return providedSelectionSetNode;
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
        var keep = ImmutableStack<ConditionedSelectionSet>.Empty;
        var anyMerged = false;

        foreach (var entry in context.Unresolvable)
        {
            var entryPath = entry.SelectionSet.Path;

            // A child entry has exactly one more segment than the current path,
            // and that extra segment is an InlineFragment.
            if (entryPath.Length == currentPath.Length + 1
                && entryPath[entryPath.Length - 1].Kind == SelectionPathSegmentKind.InlineFragment
                && currentPath.IsParentOfOrSame(entryPath)
                && currentConditions.SequenceEqual(entry.Conditions))
            {
                var typeName = entryPath[entryPath.Length - 1].Name;
                unresolvableSelections.Add(new InlineFragmentNode(
                    null,
                    new NamedTypeNode(typeName),
                    [],
                    entry.SelectionSet.Node));
                anyMerged = true;
            }
            else
            {
                keep = keep.Push(entry);
            }
        }

        if (anyMerged)
        {
            context.Unresolvable = keep;
        }
    }

    private (FieldNode?, FieldNode?) RewriteFieldNode(
        Context context,
        FusionComplexTypeDefinition complexType,
        FieldNode fieldNode,
        FieldNode? providedFieldNode)
    {
        var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

        if (providedFieldNode is null)
        {
            // if the field is not available in the current schema we return null
            // which will remove the field from the rewritten selection set.
            if (!field.Sources.TryGetMember(context.SchemaName, out var source))
            {
                return (null, fieldNode);
            }

            if (source.Requirements is not null)
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
        }

        var selectionSet = fieldNode.SelectionSet;

        if (selectionSet is not null)
        {
            context.Nodes.Push(fieldNode);

            var (resolvable, unresolvable) = RewriteSelectionSet(
                context,
                field.Type.AsTypeDefinition(),
                selectionSet,
                providedFieldNode?.SelectionSet);

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
    }

    private (InlineFragmentNode?, InlineFragmentNode?) RewriteFragmentNode(
        Context context,
        ITypeDefinition type,
        InlineFragmentNode inlineFragmentNode,
        SelectionSetNode? providedFieldNode)
    {
        // TODO: we need to implement proper type routing here later.
        var typeCondition = type;

        if (inlineFragmentNode.TypeCondition is not null)
        {
            typeCondition = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
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
                providedFieldNode);

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
}
