using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal class PropertyNodeContainer(
    List<PropertyNode>? nodes = null)
    : IPropertyNodeProvider
{
    private static readonly IReadOnlyList<PropertyNode> _emptyNodes = Array.Empty<PropertyNode>();
    private List<PropertyNode>? _nodes = nodes;
    private bool _sealed;

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
            }
        }

        _nodes.Add(newNode);
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
