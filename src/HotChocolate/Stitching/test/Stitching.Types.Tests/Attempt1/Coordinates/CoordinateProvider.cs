using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class CoordinateProvider
{
    private readonly Stack<SchemaCoordinate2> _coordinates = new();
    private readonly Dictionary<object, ISchemaNode> _coordinateToDestinationLookup = new();

    public ISchemaCoordinate2 Add(object node)
    {
        _coordinates.TryPeek(out SchemaCoordinate2 parent);
        NameNode? name = default;
        if (node is INamedSyntaxNode namedSyntaxNode)
        {
            name = namedSyntaxNode.Name;
        }

        var coordinate = new SchemaCoordinate2(parent, name);

        _coordinates.Push(coordinate);
        return coordinate;
    }

    public bool TryGet(ISchemaCoordinate2 coordinate, out ISchemaNode? schemaNode)
    {
        return _coordinateToDestinationLookup.TryGetValue(coordinate, out schemaNode);
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
