namespace HotChocolate.Execution.Projections;

internal class TypeContainer(List<TypeNode>? nodes = null)
{
    private static readonly IReadOnlyList<TypeNode> _emptyNodes = Array.Empty<TypeNode>();
    private List<TypeNode>? _nodes = nodes;
    private bool _sealed;

    public IReadOnlyList<TypeNode> Nodes
        => _nodes ?? _emptyNodes;

    public void TryAddNode(TypeNode newNode)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The property node container is sealed.");
        }

        _nodes ??= new();

        foreach (var node in _nodes)
        {
            if (node.Type.FullName!.Equals(newNode.Type.FullName!))
            {
                if (!node.Type.MetadataToken.Equals(newNode.Type.MetadataToken))
                {
                    throw new InvalidOperationException("Duplicate type name.");
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
