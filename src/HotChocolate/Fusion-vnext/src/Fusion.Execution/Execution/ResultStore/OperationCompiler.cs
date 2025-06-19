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
                    var index = 0;

                    if (includeConditions.Add(includeCondition))
                    {
                        index = includeConditions.Count - 1;
                    }
                    else
                    {
                        index = includeConditions.IndexOf(includeCondition);
                    }

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

internal readonly struct IncludeCondition(string? skip, string? include)
    : IEquatable<IncludeCondition>
{
    public string? Skip { get; } = skip;

    public string? Include { get; } = include;

    public bool Equals(IncludeCondition other)
        => string.Equals(Skip, other.Skip, StringComparison.Ordinal)
            && string.Equals(Include, other.Include, StringComparison.Ordinal);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is IncludeCondition other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Skip, Include);

    public static bool TryCreate(FieldNode field, out IncludeCondition includeCondition)
    {
        string? skip = null;
        string? include = null;

        for (var i = 0; i < field.Directives.Count; i++)
        {
            var directive = field.Directives[i];
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode skipVariable)
            {
                skip = skipVariable.Name.Value;
            }
            else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode includeVariable)
            {
                include = includeVariable.Name.Value;
            }

            if (skip is not null && include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }
        }

        includeCondition = default;
        return false;
    }
}

internal class IncludeConditionCollection : ICollection<IncludeCondition>
{
    private readonly OrderedDictionary<IncludeCondition, bool> _dictionary = [];

    public IncludeCondition this[int index]
        => _dictionary.GetAt(index).Key;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    public bool Add(IncludeCondition item)
        => _dictionary.TryAdd(item, true);

    void ICollection<IncludeCondition>.Add(IncludeCondition item)
        => Add(item);

    public bool Remove(IncludeCondition item)
        => throw new InvalidOperationException("This is an add only collection.");

    void ICollection<IncludeCondition>.Clear()
        => throw new InvalidOperationException("This is an add only collection.");

    public bool Contains(IncludeCondition item)
        => _dictionary.ContainsKey(item);

    public int IndexOf(IncludeCondition item)
       => _dictionary.IndexOf(item);

    public void CopyTo(IncludeCondition[] array, int arrayIndex)
        => _dictionary.Keys.CopyTo(array, arrayIndex);

    public IEnumerator<IncludeCondition> GetEnumerator()
        => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}