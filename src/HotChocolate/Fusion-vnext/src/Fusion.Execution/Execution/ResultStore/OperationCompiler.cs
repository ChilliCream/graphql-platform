using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

            CollectFields(
                operationDefinition.SelectionSet.Selections,
                _schema.GetOperationType(operationDefinition.Operation),
                fields);

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
                var includeFlags = 0ul;

                if (!fields.TryGetValue(responseName, out var builder))
                {
                    builder = [];
                    fields.Add(responseName, builder);
                }

                if (IncludeCondition.TryCreate(fieldNode, out var includeCondition))
                {
                    var index = includeConditions.Add(includeCondition)
                        ? includeConditions.Count - 1
                        : includeConditions.IndexOf(includeCondition);
                    includeFlags |= 1ul << index;
                }

                builder.Add(new FieldSelectionNode(fieldNode, includeFlags));
            }

            if (selection is InlineFragmentNode inlineFragmentNode
                && DoesTypeApply(inlineFragmentNode.TypeCondition, typeContext))
            {
                CollectFields(
                    inlineFragmentNode.SelectionSet.Selections,
                    typeContext,
                    fields);
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

        foreach (var (responseName, syntaxNodes) in fieldMap)
        {
            var firstNode = syntaxNodes[0].Node;
            var includeFlags = syntaxNodes[0].IncludeFlags;

            if (syntaxNodes.Count > 1)
            {
                for (var j = 1; j < syntaxNodes.Count; j++)
                {
                    includeFlags |= syntaxNodes[j].IncludeFlags;
                    if (!firstNode.Name.Value.Equals(syntaxNodes[j].Node.Name.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"The syntax nodes for the response name {responseName} are not all the same.");
                    }
                }
            }

            var field = typeContext.Fields[syntaxNodes[0].Node.Name.Value];

            selections[i++] = new Selection(
                ++lastId,
                responseName,
                field,
                syntaxNodes.ToArray(),
                includeFlags);

            if (includeFlags is not 0)
            {
                isConditional = true;
            }
        }

        return new SelectionSet(++lastId, selections, isConditional);
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
