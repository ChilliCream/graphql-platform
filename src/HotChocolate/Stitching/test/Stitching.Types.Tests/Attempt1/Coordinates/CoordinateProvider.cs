using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class CoordinateProvider : ISchemaCoordinate2Provider
{
    private readonly Dictionary<object, ISchemaNode> _coordinateToDestinationLookup = new();
    private readonly Dictionary<ISchemaNode, ISchemaCoordinate2> _nodeToCoordinateLookup = new();
    private readonly Dictionary<ISyntaxNode, ISchemaCoordinate2> _syntaxNodeToCoordinateLookup = new();

    public ISchemaNode Root => _coordinateToDestinationLookup.Values.First();

    public ISchemaCoordinate2 CalculateCoordinate(ISchemaNode node)
    {
        return CalculateCoordinate(node.Parent, node.Definition);
    }

    public ISchemaCoordinate2 CalculateCoordinate(ISchemaNode? parent, ISyntaxNode node)
    {
        NameNode? name = node switch
        {
            IHasName namedSyntaxNode => namedSyntaxNode.Name,
            DocumentNode => SchemaCoordinatePrinter.Root,
            _ => default
        };

        TryGet(parent, out ISchemaCoordinate2? parentCoordinate);
        var coordinate = new SchemaCoordinate2(this,
            parentCoordinate,
            node.Kind,
            name);

        return coordinate;
    }

    public ISchemaCoordinate2 Add(ISchemaNode node)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(node);
        Associate(coordinate, node);

        return coordinate;
    }

    public bool TryGet(ISchemaNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate)
    {
        if (node is null)
        {
            coordinate = default;
            return false;
        }

        return _nodeToCoordinateLookup.TryGetValue(node, out coordinate);
    }

    public bool TryGet(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaNode? schemaNode)
    {
        if (node is null)
        {
            schemaNode = default;
            return false;
        }

        if (!TryGet(node, out ISchemaCoordinate2? coordinate))
        {
            schemaNode = default;
            return false;
        }

        return _coordinateToDestinationLookup.TryGetValue(coordinate, out schemaNode);
    }

    public ISchemaCoordinate2? Get(ISchemaNode node)
    {
        return !TryGet(node, out ISchemaCoordinate2? coordinate)
            ? default
            : coordinate;
    }

    public bool TryGet(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate)
    {
        if (node is null)
        {
            coordinate = default;
            return false;
        }

        if (node.Kind == SyntaxKind.Document)
        {
            coordinate = Root.Coordinate;
            return true;
        }

        return _syntaxNodeToCoordinateLookup.TryGetValue(node, out coordinate);
    }

    public ISchemaCoordinate2? Get(ISyntaxNode node)
    {
        return !TryGet(node, out ISchemaCoordinate2? coordinate)
            ? default
            : coordinate;
    }

    public ISchemaNode? Get(ISchemaCoordinate2? coordinate)
    {
        if (coordinate is null)
        {
            return default;
        }

        return !TryGet(coordinate, out ISchemaNode? schemaNode)
            ? default
            : schemaNode;
    }

    public bool TryGet(ISchemaCoordinate2? coordinate, [NotNullWhen(true)] out ISchemaNode? schemaNode)
    {
        if (coordinate is null)
        {
            schemaNode = default;
            return false;
        }

        return _coordinateToDestinationLookup.TryGetValue(coordinate, out schemaNode);
    }

    public void Associate<TDefinition>(ISchemaCoordinate2 coordinate, TDefinition typedDestination)
        where TDefinition : ISchemaNode
    {
        _nodeToCoordinateLookup[typedDestination] = coordinate;
        _syntaxNodeToCoordinateLookup[typedDestination.Definition] = coordinate;
        _coordinateToDestinationLookup[coordinate] = typedDestination;
    }
}
