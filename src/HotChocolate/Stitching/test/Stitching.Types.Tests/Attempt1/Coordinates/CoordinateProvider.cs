using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class CoordinateProvider
{
    private readonly Stack<ISchemaCoordinate2> _coordinates = new();
    private readonly Dictionary<object, ISchemaNode> _coordinateToDestinationLookup = new();
    private readonly Dictionary<ISyntaxNode, ISchemaCoordinate2> _nodeToCoordinateLookup = new();

    public ISchemaNode Root => _coordinateToDestinationLookup.Values.First();

    public ISchemaCoordinate2 Add(ISyntaxNode node)
    {
        NameNode? name = default;
        if (node is IHasName namedSyntaxNode)
        {
            name = namedSyntaxNode.Name;
        }

        _coordinates.TryPeek(out ISchemaCoordinate2? parent);
        var coordinate = new SchemaCoordinate2(parent, node.Kind, name);

        _nodeToCoordinateLookup[node] = coordinate;
        _coordinates.Push(coordinate);
        return coordinate;
    }

    public bool TryGet(ISchemaCoordinate2 coordinate, [NotNullWhen(true)] out ISchemaNode? schemaNode)
    {
        return _coordinateToDestinationLookup.TryGetValue(coordinate, out schemaNode);
    }

    public ISchemaCoordinate2? Get(ISyntaxNode node)
    {
        return _nodeToCoordinateLookup.TryGetValue(node, out ISchemaCoordinate2? coordinate)
            ? coordinate
            : default;
    }

    public void Associate<TDefinition>(ISchemaCoordinate2 coordinate, TDefinition typedDestination)
        where TDefinition : ISchemaNode
    {
        _coordinateToDestinationLookup[coordinate] = typedDestination;
    }

    public void Remove()
    {
        _coordinates.TryPop(out _);
    }
}
