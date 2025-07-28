using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Collections;

internal sealed class SatisfiabilityPath : IEnumerable<ISatisfiabilityPathItem>
{
    private readonly Stack<ISatisfiabilityPathItem> _stack = [];
    private readonly HashSet<ISatisfiabilityPathItem> _hashSet = [];

    public bool Contains(ISatisfiabilityPathItem item)
    {
        return _hashSet.Contains(item);
    }

    public int Count => _stack.Count;

    public bool Push(ISatisfiabilityPathItem item)
    {
        if (_hashSet.Contains(item))
        {
            return false;
        }

        _stack.Push(item);
        _hashSet.Add(item);

        return true;
    }

    public ISatisfiabilityPathItem Pop()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        var item = _stack.Pop();
        _hashSet.Remove(item);

        return item;
    }

    public ISatisfiabilityPathItem Peek()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        return _stack.Peek();
    }

    public bool TryPeek([MaybeNullWhen(false)] out ISatisfiabilityPathItem item)
    {
        if (_stack.Count == 0)
        {
            item = null;
            return false;
        }

        item = _stack.Peek();
        return true;
    }

    public void Clear()
    {
        _stack.Clear();
        _hashSet.Clear();
    }

    public IEnumerator<ISatisfiabilityPathItem> GetEnumerator()
    {
        return _stack.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return string.Join(" -> ", _stack.Reverse());
    }
}

internal interface ISatisfiabilityPathItem
{
    string SchemaName { get; }
}

internal sealed record SatisfiabilityPathItem(
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    string SchemaName) : ISatisfiabilityPathItem
{
    public ITypeDefinition FieldType { get; } = Field.Type.AsTypeDefinition();

    private readonly int _hashCode = HashCode.Combine(Field, Type, SchemaName);

    public override string ToString()
    {
        return $"{SchemaName}:{Type.Name}.{Field.Name}<{FieldType.Name}>";
    }

    public override int GetHashCode() => _hashCode;
}

internal sealed class NodeSatisfiabilityPathItem : ISatisfiabilityPathItem
{
    private readonly MutableOutputFieldDefinition _nodeField;
    private readonly MutableObjectTypeDefinition _queryType;
    private readonly FieldDefinitionNode _lookupFieldDefinition;
    private readonly int _hashCode;

    public NodeSatisfiabilityPathItem(
        MutableOutputFieldDefinition nodeField,
        MutableObjectTypeDefinition queryType,
        IDirective lookupDirective)
    {
        _nodeField = nodeField;
        _queryType = queryType;

        var fieldDirectiveArgument = (string)lookupDirective.Arguments[WellKnownArgumentNames.Field].Value!;
        _lookupFieldDefinition = Utf8GraphQLParser.Syntax.ParseFieldDefinition(fieldDirectiveArgument);

        SchemaName = (string)lookupDirective.Arguments[WellKnownArgumentNames.Schema].Value!;

        _hashCode = HashCode.Combine(nodeField, queryType, SchemaName);
    }

    public string SchemaName { get; }

    public override string ToString()
    {
        var nodeFieldType = _nodeField.Type.AsTypeDefinition();
        var lookupFieldType = _lookupFieldDefinition.Type.NamedType();

        return
            $"{_queryType.Name}.{_nodeField.Name}<{nodeFieldType.Name}> -> "
            + $"{SchemaName}:{_queryType.Name}.{_lookupFieldDefinition.Name}<{lookupFieldType.Name}>";
    }

    public override int GetHashCode() => _hashCode;
}
