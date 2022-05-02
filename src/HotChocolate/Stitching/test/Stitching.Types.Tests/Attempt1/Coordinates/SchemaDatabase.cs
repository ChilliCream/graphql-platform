using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Traversal;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1.Coordinates;

internal class SchemaDatabase : ISchemaDatabase
{
    private readonly Dictionary<ISchemaCoordinate2, ISchemaNode> _coordinateToSchemaNodeLookup = new();
    private readonly HashSet<ISchemaNode> _nodes = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ISchemaNode, ISchemaCoordinate2> _nodeToCoordinateLookup = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ISyntaxNode, ISchemaCoordinate2> _syntaxNodeToCoordinateLookup = new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<SyntaxKind, SyntaxKind> _syntaxMap = new()
    {
        { SyntaxKind.ObjectTypeExtension, SyntaxKind.ObjectTypeDefinition },
        { SyntaxKind.InterfaceTypeExtension, SyntaxKind.InterfaceTypeDefinition }
    };

    public SchemaDatabase(string? name = default)
    {
        Name = name;
    }

    public string? Name { get; }

    public ISchemaNode Root => _coordinateToSchemaNodeLookup.Values.First();

    private ISchemaCoordinate2 CalculateCoordinate(ISchemaNode? parent, ISyntaxNode node)
    {
        TryGet(parent, out ISchemaCoordinate2? parentCoordinate);
        return CalculateCoordinate(parentCoordinate, node);
    }

    public ISchemaCoordinate2 CalculateCoordinate(ISchemaCoordinate2? parentCoordinate, ISyntaxNode node)
    {
        ISchemaCoordinate2 coordinate = node switch
        {
            DocumentNode => CalculateCoordinate(default, node.Kind, Name is not null ? new NameNode(Name) : default),
            NameNode nameNode => CalculateCoordinate(parentCoordinate, node.Kind, nameNode),
            ArgumentNode argumentNode => CalculateCoordinate(parentCoordinate, node.Kind, argumentNode.Name),
            IValueNode valueNode => CalculateCoordinate(parentCoordinate, node.Kind, new NameNode($"{valueNode.Kind:G}")),
            IHasName namedSyntaxNode => CalculateCoordinate(parentCoordinate, node.Kind, namedSyntaxNode.Name),
            ITypeNode typeNode => CalculateCoordinate(parentCoordinate, node.Kind, new NameNode($"{typeNode.Kind:G}")),
            _ => throw new NotImplementedException()
        };

        return coordinate;
    }

    private ISchemaCoordinate2 CalculateCoordinate(ISchemaCoordinate2? parentCoordinate, SyntaxKind kind, NameNode? name = default)
    {
        if (_syntaxMap.TryGetValue(kind, out SyntaxKind alternativeKind))
        {
            kind = alternativeKind;
        }

        return new SchemaCoordinate2(
            parentCoordinate,
            kind,
            name);
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

    public bool TryGet(
        ISchemaNode? parent,
        ISyntaxNode node,
        [MaybeNullWhen(false)]  out ISchemaNode schemaNode)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(parent, node);
        return TryGet(coordinate, out schemaNode);
    }

    public bool TryGet(ISchemaNode? node, [MaybeNullWhen(false)] out ISchemaCoordinate2 coordinate)
    {
        if (node is null)
        {
            coordinate = default;
            return false;
        }

        return _nodeToCoordinateLookup.TryGetValue(node, out coordinate);
    }

    public bool TryGet(ISyntaxNode? parent, ISyntaxNode node, [MaybeNullWhen(false)] out ISchemaNode existingNode)
    {
        ISchemaCoordinate2 coordinate = CalculateCoordinate(parent, node);

        return TryGet(coordinate, out existingNode);
    }

    public bool TryGet(ISchemaCoordinate2? coordinate, [MaybeNullWhen(false)] out ISchemaNode schemaNode)
    {
        if (coordinate is null)
        {
            schemaNode = default;
            return false;
        }

        return _coordinateToSchemaNodeLookup.TryGetValue(coordinate, out schemaNode);
    }

    public bool TryGetExact(ISyntaxNode? node, [MaybeNullWhen(false)] out ISchemaNode schemaNode)
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

    public ISchemaNode GetOrAdd(ISchemaNode node)
    {
        if (TryGet(node.Coordinate, out ISchemaNode? existingNode))
        {
            return existingNode;
        }

        ISchemaNode? existingParentNode = default;
        if (node.Parent?.Coordinate is not null && !TryGet(node.Parent?.Coordinate, out existingParentNode))
        {
            throw new InvalidOperationException();
        }

        ISchemaNode newNode = SchemaNodeFactory.CreateEmpty(this, existingParentNode, node.Definition);

        Reindex(newNode);
        return newNode;
    }

    public ISchemaNode GetOrAdd(ISchemaNode parent, ISyntaxNode node)
    {
        return GetOrAdd(parent.Coordinate, node);
    }

    public ISchemaNode GetOrAdd(ISchemaCoordinate2? coordinate, ISyntaxNode node)
    {
        if (TryGet(coordinate, out ISchemaNode? existingNode))
        {
            return existingNode;
        }

        if (!TryGet(coordinate?.Parent, out ISchemaNode? parentNode))
        {
            throw new InvalidOperationException();
        }

        ISchemaNode schemaNode = SchemaNodeFactory.Create(this, parentNode, node);
        Reindex(schemaNode);
        return schemaNode;
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
            if (!TryGet(nodeReference.Parent?.Node, nodeReference.Node, out ISchemaNode? existingNode))
            {
                ISchemaNode newNode = CreateSchemaNode(nodeReference.Parent?.Node, nodeReference.Node);
                existingNode = newNode;
            }

            Reindex(existingNode);
        }

        return schemaNode;
    }

    public ISchemaNode CreateSchemaNode(ISyntaxNode? parent, ISyntaxNode node)
    {
        ISchemaNode? parentSchemaNode = default;
        if (parent is not null)
        {
            if (!TryGetExact(parent, out parentSchemaNode))
            {
                throw new InvalidOperationException("Parent missing");
            }
        }

        return SchemaNodeFactory.Create(this, parentSchemaNode, node);
    }

    private void InnerReindex<TDefinition>(TDefinition typedDestination)
        where TDefinition : ISchemaNode
    {
        ISchemaCoordinate2 coordinate =
            CalculateCoordinate(typedDestination.Coordinate?.Parent, typedDestination.Definition);

        _nodes.AddIfNotExist((ISchemaNode) typedDestination);
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
}
