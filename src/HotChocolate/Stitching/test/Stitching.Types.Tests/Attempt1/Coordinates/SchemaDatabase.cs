using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Traversal;

namespace HotChocolate.Stitching.Types.Attempt1.Coordinates;

internal class SchemaDatabase : ISchemaDatabase
{
    private readonly SchemaNodeFactory _schemaNodeFactory;
    private readonly Dictionary<ISchemaCoordinate2, ISchemaNode> _coordinateToSchemaNodeLookup = new();
    private readonly Dictionary<ISchemaNode, ISchemaCoordinate2> _nodeToCoordinateLookup = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ISyntaxNode, ISchemaCoordinate2> _syntaxNodeToCoordinateLookup = new(ReferenceEqualityComparer.Instance);

    public ISchemaNode Root => _coordinateToSchemaNodeLookup.Values.First();

    public SchemaDatabase(SchemaNodeFactory schemaNodeFactory)
    {
        _schemaNodeFactory = schemaNodeFactory;
    }

    public ISchemaCoordinate2 Add(ISchemaNode node)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(node);
        InnerReindex(node);

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

    public bool TryGet(ISchemaNode? parent, ISyntaxNode node, [NotNullWhen(true)] out ISchemaNode? schemaNode)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(parent, node);

        return TryGet(coordinate, out schemaNode);
    }

    public bool TryGet(ISyntaxNode? parent, ISyntaxNode node, [NotNullWhen(true)] out ISchemaNode? existingNode)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(parent, node);

        return _coordinateToSchemaNodeLookup.TryGetValue(coordinate, out existingNode);
    }

    public bool TryGetExact(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaNode? schemaNode)
    {
        if (node is null)
        {
            schemaNode = default;
            return false;
        }

        if (!TryGetExact(node, out ISchemaCoordinate2? coordinate))
        {
            schemaNode = default;
            return false;
        }

        return _coordinateToSchemaNodeLookup.TryGetValue(coordinate, out schemaNode);
    }

    public ISchemaCoordinate2? Get(ISchemaNode node)
    {
        return !TryGet(node, out ISchemaCoordinate2? coordinate)
            ? default
            : coordinate;
    }

    public bool TryGetExact(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate)
    {
        if (node is null)
        {
            coordinate = default;
            return false;
        }

        if (node.Kind == SyntaxKind.Document)
        {
            Reindex(node, Root);
            coordinate = Root.Coordinate!;
            return true;
        }

        return _syntaxNodeToCoordinateLookup.TryGetValue(node, out coordinate);
    }

    public ISchemaCoordinate2? Get(ISyntaxNode node)
    {
        return !TryGetExact(node, out ISchemaCoordinate2? coordinate)
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

        return _coordinateToSchemaNodeLookup.TryGetValue(coordinate, out schemaNode);
    }

    public ISchemaNode Reindex(ISyntaxNode? parent, ISyntaxNode node)
    {
        if (!TryGet(parent, node, out ISchemaNode? schemaNode))
        {
            schemaNode = CreateSchemaNode(parent, node);
        }

        return Reindex(schemaNode);
    }

    public ISchemaNode Reindex(ISchemaNode schemaNode)
    {
        InnerReindex(schemaNode);

        IEnumerable<SyntaxNodeReference> childSyntaxNodes = schemaNode.ChildSyntaxNodes();

        foreach (SyntaxNodeReference nodeReference in childSyntaxNodes)
        {
            if (!TryGet(nodeReference.Parent.Node, nodeReference.Node, out ISchemaNode? existingNode))
            {
                ISchemaNode node = CreateSchemaNode(nodeReference.Parent.Node, nodeReference.Node);
                existingNode = node;
            }

            Reindex(existingNode);
        }

        return schemaNode;
    }

    private ISchemaCoordinate2 CalculateCoordinate(ISyntaxNode? parent, ISyntaxNode node)
    {
        ISchemaNode? parentSchemaNode = default;

        if (parent is not null)
        {
            if (!TryGetExact(parent, out ISchemaCoordinate2? parentCoordinate))
            {
                throw new InvalidOperationException();
            }

            if (!TryGet(parentCoordinate, out parentSchemaNode))
            {
                throw new InvalidOperationException();
            }
        }

        return CalculateCoordinate(parentSchemaNode, node);
    }

    public ISchemaCoordinate2 CalculateCoordinate(ISchemaNode node)
    {
        return CalculateCoordinate(node.Parent, node.Definition);
    }

    private ISchemaNode CreateSchemaNode(ISyntaxNode? parent, ISyntaxNode node)
    {
        ISchemaNode? parentSchemaNode = default;
        if (parent is not null)
        {
            if (!TryGetExact(parent, out parentSchemaNode))
            {
                throw new InvalidOperationException("Parent missing");
            }
        }

        if (_schemaNodeFactory.Create(parentSchemaNode, node, Add, out ISchemaNode nodeSchemaNode))
        {
            nodeSchemaNode.RewriteDefinition(nodeSchemaNode.Definition);
        }

        return nodeSchemaNode;
    }

    private void InnerReindex<TDefinition>(TDefinition typedDestination)
        where TDefinition : ISchemaNode
    {
        ISchemaCoordinate2 coordinate =
            CalculateCoordinate(typedDestination.Coordinate?.Parent, typedDestination.Definition);

        _nodeToCoordinateLookup[typedDestination] = coordinate;
        _coordinateToSchemaNodeLookup[coordinate] = typedDestination;

        Reindex(typedDestination.Definition, coordinate);
    }

    private void Reindex(ISyntaxNode source, ISchemaNode destination)
    {
        Reindex(source, destination.Coordinate);
    }

    private void Reindex(ISyntaxNode source, ISchemaCoordinate2? coordinate)
    {
        _syntaxNodeToCoordinateLookup[source] = coordinate
                                                ?? throw new ArgumentNullException(nameof(coordinate));
    }

    private ISchemaCoordinate2 CalculateCoordinate(ISchemaNode? parent, ISyntaxNode node)
    {
        TryGet(parent, out ISchemaCoordinate2? parentCoordinate);
        return CalculateCoordinate(parentCoordinate, node);
    }

    private ISchemaCoordinate2 CalculateCoordinate(ISchemaCoordinate2? parentCoordinate, ISyntaxNode node)
    {
        ISchemaCoordinate2 coordinate = node switch
        {
            DocumentNode => CreateCoordinate(default, node, new NameNode("Root")),
            NameNode nameNode => CreateCoordinate(parentCoordinate, node, nameNode),
            ArgumentNode argumentNode => CreateCoordinate(parentCoordinate, node, argumentNode.Name),
            IValueNode valueNode => CreateCoordinate(parentCoordinate, node, new NameNode($"{valueNode.Kind:G}")),
            IHasName namedSyntaxNode => CreateCoordinate(parentCoordinate, node, namedSyntaxNode.Name),
            ITypeNode typeNode => CreateCoordinate(parentCoordinate, node, new NameNode($"{typeNode.Kind:G}")),
            _ => throw new NotImplementedException()
        };

        return coordinate;
    }

    private SchemaCoordinate2 CreateCoordinate(ISchemaCoordinate2? parentCoordinate, ISyntaxNode node, NameNode name)
    {
        return new SchemaCoordinate2(this,
            parentCoordinate,
            node.Kind,
            name);
    }
}
