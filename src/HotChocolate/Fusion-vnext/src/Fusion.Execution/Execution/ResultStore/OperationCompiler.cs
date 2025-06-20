using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class OperationCompiler
{
    private readonly ISchemaDefinition _schema;
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldsPool;

    public OperationCompiler(
        ISchemaDefinition schema,
        ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> fieldsPool)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fieldsPool);

        _schema = schema;
        _fieldsPool = fieldsPool;
    }

    public void Compile(OperationDefinitionNode operationDefinition)
    {
        var fields = _fieldsPool.Get();

        try
        {
            var lastId = 0u;
            var parentIncludeFlags = 0ul;
            var includeConditions = new IncludeConditionCollection();

            CollectFields(
                parentIncludeFlags,
                operationDefinition.SelectionSet.Selections,
                _schema.GetOperationType(operationDefinition.Operation),
                fields,
                includeConditions);

            var selectionSet = BuildSelectionSet(
                fields,
                _schema.GetOperationType(operationDefinition.Operation),
                ref lastId);
        }
        finally
        {
            _fieldsPool.Return(fields);
        }
    }

    private void CollectFields(
        ulong parentIncludeFlags,
        IReadOnlyList<ISelectionNode> selections,
        IObjectTypeDefinition typeContext,
        OrderedDictionary<string, List<FieldSelectionNode>> fields,
        IncludeConditionCollection includeConditions)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (selection is FieldNode fieldNode)
            {
                var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;
                var pathIncludeFlags = parentIncludeFlags;

                if (!fields.TryGetValue(responseName, out var nodes))
                {
                    nodes = [];
                    fields.Add(responseName, nodes);
                }

                if (IncludeCondition.TryCreate(fieldNode, out var includeCondition))
                {
                    var index = includeConditions.Add(includeCondition)
                        ? includeConditions.Count - 1
                        : includeConditions.IndexOf(includeCondition);
                    pathIncludeFlags |= 1ul << index;
                }

                nodes.Add(new FieldSelectionNode(fieldNode, pathIncludeFlags));
            }

            if (selection is InlineFragmentNode inlineFragmentNode
                && DoesTypeApply(inlineFragmentNode.TypeCondition, typeContext))
            {
                var pathIncludeFlags = parentIncludeFlags;

                if (IncludeCondition.TryCreate(inlineFragmentNode, out var includeCondition))
                {
                    var index = includeConditions.Add(includeCondition)
                        ? includeConditions.Count - 1
                        : includeConditions.IndexOf(includeCondition);
                    pathIncludeFlags |= 1ul << index;
                }

                CollectFields(
                    pathIncludeFlags,
                    inlineFragmentNode.SelectionSet.Selections,
                    typeContext,
                    fields,
                    includeConditions);
            }
        }
    }

    public SelectionSet BuildSelectionSet(
        OrderedDictionary<string, List<FieldSelectionNode>> fieldMap,
        IObjectTypeDefinition typeContext,
        ref uint lastId)
    {
        var i = 0;
        var selections = new Selection[fieldMap.Count];
        var isConditional = false;
        var includeFlags = new List<ulong>();

        foreach (var (responseName, nodes) in fieldMap)
        {
            includeFlags.Clear();

            var first = nodes[0];

            if (first.PathIncludeFlags > 0)
            {
                includeFlags.Add(first.PathIncludeFlags);
            }

            if (nodes.Count > 1)
            {
                for (var j = 1; j < nodes.Count; j++)
                {
                    var next = nodes[j];

                    if (!first.Node.Name.Value.Equals(next.Node.Name.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"The syntax nodes for the response name {responseName} are not all the same.");
                    }

                    if (next.PathIncludeFlags > 0)
                    {
                        includeFlags.Add(next.PathIncludeFlags);
                    }
                }
            }

            if (includeFlags.Count > 1)
            {
                CollapseIncludeFlags(includeFlags);
            }

            var field = typeContext.Fields[first.Node.Name.Value];

            selections[i++] = new Selection(
                ++lastId,
                responseName,
                field,
                nodes.ToArray(),
                includeFlags.ToArray());

            if (includeFlags.Count > 1)
            {
                isConditional = true;
            }
        }

        return new SelectionSet(++lastId, selections, isConditional);
    }

    private static void CollapseIncludeFlags(List<ulong> includeFlags)
    {
        // we sort the include flags to improve early elimination and stability
        includeFlags.Sort();

        var write = 0;

        for (var read = 0; read < includeFlags.Count; read++)
        {
            var candidate = includeFlags[read];
            var covered = false;

            // we check if the candidate is already covered
            for (var i = 0; i < write; i++)
            {
                if ((candidate & includeFlags[i]) == includeFlags[i])
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
            {
                // lastly we remove more restrictive flags from the already written range
                for (var i = 0; i < write;)
                {
                    if ((includeFlags[i] & candidate) == candidate)
                    {
                        includeFlags[i] = includeFlags[--write];
                    }
                    else
                    {
                        i++;
                    }
                }

                if (write < read)
                {
                    includeFlags[write] = candidate;
                }
                write++;
            }
        }

        // we trim the list to the collapsed set
        if (write < includeFlags.Count)
        {
            includeFlags.RemoveRange(write, includeFlags.Count - write);
        }
    }

    private bool DoesTypeApply(NamedTypeNode? typeCondition, IObjectTypeDefinition typeContext)
    {
        if (typeCondition is null)
        {
            return true;
        }

        if (typeCondition.Name.Value.Equals(typeContext.Name, StringComparison.Ordinal))
        {
            return true;
        }

        if (_schema.Types.TryGetType(typeCondition.Name.Value, out var type))
        {
            return type.IsAssignableFrom(typeContext);
        }

        return false;
    }
}
