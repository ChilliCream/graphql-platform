using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class RootSelectionSetPartitioner(FusionSchemaDefinition schema)
{
    public RootSelectionSetPartitionerResult Partition(RootSelectionSetPartitionerInput input)
    {
        var context = new Context(input.SelectionSetIndex);

        var prunedRoot =
            RewriteRootSelectionSet(
                type: input.SelectionSet.Type,
                node: input.SelectionSet.Node,
                out var nodeFields);

        // If nothing but node(...) remained, SelectionSet should be null.
        SelectionSet? prunedSelectionSet = null;

        if (prunedRoot?.Selections.Count > 0)
        {
            // Register rewritten node in the index for downstream mappings.
            context.Register(input.SelectionSet.Node, prunedRoot);

            var id = context.GetId(prunedRoot);
            prunedSelectionSet = new SelectionSet(
                id,
                prunedRoot,
                input.SelectionSet.Type,
                input.SelectionSet.Path);
        }

        return new RootSelectionSetPartitionerResult(prunedSelectionSet, nodeFields);
    }

    /// <summary>
    /// Removes all Query.node(...) fields from the given selection set (including those inside
    /// inline fragments that wrap the root scope). Preserves structure and directives, and
    /// drops fragments that become empty. Returns the (possibly) rewritten SelectionSetNode.
    /// </summary>
    private SelectionSetNode? RewriteRootSelectionSet(
        ITypeDefinition type,
        SelectionSetNode node,
        out List<FieldNode> nodeFields)
    {
        nodeFields = new List<FieldNode>();

        List<ISelectionNode>? kept = null; // null => identical to original so far

        for (var i = 0; i < node.Selections.Count; i++)
        {
            var sel = node.Selections[i];

            switch (sel)
            {
                case FieldNode field:
                    if (IsQueryNodeField(schema, type, field))
                    {
                        // Collect the root-level node field and remove it from the result.
                        nodeFields.Add(field);
                        EnsureKeptInitializedUpToOriginal(ref kept, node, i);
                        continue;
                    }

                    // Keep non-node fields verbatim (including their nested selection sets).
                    kept?.Add(field);
                    break;

                case InlineFragmentNode ifrag:
                    // Determine fragment scope: either the type condition or the current scope.
                    var scope = type;
                    if (ifrag.TypeCondition is not null)
                    {
                        scope = schema.Types[ifrag.TypeCondition.Name.Value];
                    }

                    // Recurse into the fragment selection set (still root scope wrappers).
                    var rewrittenChild =
                        RewriteRootSelectionSet(scope, ifrag.SelectionSet, out var childNodeFields);

                    if (childNodeFields.Count > 0)
                    {
                        nodeFields.AddRange(childNodeFields);
                    }

                    // If the fragment became empty, drop it.
                    if (rewrittenChild is null || rewrittenChild.Selections.Count == 0)
                    {
                        EnsureKeptInitializedUpToOriginal(ref kept, node, i);
                        continue;
                    }

                    // If anything changed under this fragment, update just its selection set.
                    if (!ReferenceEquals(rewrittenChild, ifrag.SelectionSet))
                    {
                        var newFrag = ifrag.WithSelectionSet(rewrittenChild);

                        EnsureKeptInitializedUpToOriginal(ref kept, node, i);
                        kept!.Add(newFrag);
                    }
                    else
                    {
                        kept?.Add(ifrag);
                    }
                    break;

                default:
                    // Fragment spreads (and other selection kinds) at the root are preserved.
                    kept?.Add(sel);
                    break;
            }
        }

        // If we never changed anything and collected no node fields, return original node.
        if (kept is null)
        {
            return nodeFields.Count == 0 ? node : new SelectionSetNode(System.Array.Empty<ISelectionNode>());
        }

        // Return the pruned selection set (may still be empty; caller decides whether to build SelectionSet)
        return new SelectionSetNode(kept);
    }

    private static bool IsQueryNodeField(
        FusionSchemaDefinition compositeSchema,
        ITypeDefinition scopeType,
        FieldNode fieldNode)
    {
        // We only remove Query.node(...) that returns the Node interface.
        if (!ReferenceEquals(scopeType, compositeSchema.QueryType))
        {
            return false;
        }

        if (fieldNode.Name.Value != "node")
        {
            return false;
        }

        if (scopeType is not FusionComplexTypeDefinition complex)
        {
            return true; // fail-open: treat "node" as the target at query scope
        }

        // Validate the field definition when available.
        var fieldDef = complex.Fields[fieldNode.Name.Value];
        var fieldTypeDef = fieldDef.Type.AsTypeDefinition();
        return fieldTypeDef is IInterfaceTypeDefinition iface && iface.Name == "Node";
    }

    private static void EnsureKeptInitializedUpToOriginal(
        ref List<ISelectionNode>? kept,
        SelectionSetNode original,
        int upToIndexExclusive)
    {
        if (kept is null)
        {
            kept = new List<ISelectionNode>(original.Selections.Count);
            for (var j = 0; j < upToIndexExclusive; j++)
            {
                kept.Add(original.Selections[j]);
            }
        }
    }

    private sealed class Context
    {
        public Context(ISelectionSetIndex selectionSetIndex)
        {
            SelectionSetIndex = selectionSetIndex;
        }

        public ISelectionSetIndex SelectionSetIndex { get; private set; }

        [field: System.Diagnostics.CodeAnalysis.AllowNull, System.Diagnostics.CodeAnalysis.MaybeNull]
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
