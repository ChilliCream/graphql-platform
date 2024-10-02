using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal sealed class TypeNode(Type type, List<PropertyNode>? nodes = null)
{
    private static readonly IReadOnlyList<PropertyNode> _emptyNodes = Array.Empty<PropertyNode>();
    private List<PropertyNode>? _nodes = nodes ?? [];
    private bool _sealed;

    public Type Type => type;

    public IReadOnlyList<PropertyNode> Nodes
        => _nodes ?? _emptyNodes;

    public PropertyNode AddOrGetNode(PropertyInfo property)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The property node container is sealed.");
        }

        _nodes ??= new();

        foreach (var node in Nodes)
        {
            if (node.Property.Name.Equals(property.Name))
            {
                return node;
            }
        }

        var newNode = new PropertyNode(property);
        _nodes.Add(newNode);
        return newNode;
    }

    public void TryAddNode(PropertyNode newNode)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The property node container is sealed.");
        }

        _nodes ??= new();

        foreach (var node in _nodes)
        {
            if (node.Property.Name.Equals(newNode.Property.Name))
            {
                if (!node.Property.MetadataToken.Equals(newNode.Property.MetadataToken))
                {
                    throw new InvalidOperationException("Duplicate property name.");
                }

                // we add the child nodes that are not already present
                foreach (var newChild in newNode.Nodes)
                {
                    node.TryAddNode(newChild);
                }

                return;
            }
        }

        _nodes.Add(newNode);
    }

    public TypeNode Clone()
    {
        List<PropertyNode>? nodes = null;

        if (Nodes.Count > 0)
        {
            nodes = new(Nodes.Count);
            foreach (var node in Nodes)
            {
                nodes.Add(node.Clone());
            }
        }

        return new TypeNode(Type, nodes);
    }

    public void Seal()
    {
        if (!_sealed)
        {
            foreach (var node in Nodes)
            {
                node.Seal();
            }

            _sealed = true;
        }
    }
}
