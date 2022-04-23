using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;

namespace HotChocolate.Stitching.Types;

internal static class SchemaNodeFactory
{
    private static readonly Dictionary<Type, Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode>?> _factories = new()
    {
        {
            typeof(DocumentNode), FactoryBuilder<DocumentNode, DocumentDefinition>(
                (coordinateFactory, parent, node) =>
                    new DocumentDefinition(
                        coordinateFactory.Invoke(parent?.Coordinate, node),
                        node))
        },
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (coordinateFactory, parent, node) =>
                    new ObjectTypeDefinition(
                        coordinateFactory.Invoke(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (coordinateFactory, parent, node) =>
                    new ObjectTypeDefinition(
                        coordinateFactory.Invoke(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(InterfaceTypeDefinitionNode), FactoryBuilder<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>(
                (coordinateFactory, parent, node) =>
                    new InterfaceTypeDefinition(
                        coordinateFactory.Invoke(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>(
                (coordinateFactory, parent, node) =>
                    new FieldDefinition(
                        coordinateFactory.Invoke(parent?.Coordinate, node),
                        (ITypeDefinition)parent!,
                        node))
        },
    };

    public static DocumentDefinition CreateDocument(SchemaDatabase database, DocumentNode node)
    {
        return Create<DocumentDefinition>(database, default, node);
    }

    public static TDefinition Create<TDefinition>(
        ISchemaDatabase database,
        ISchemaNode? parent,
        ISyntaxNode node)
        where TDefinition : ISchemaNode
    {
        Type nodeType = node.GetType();

        if (!_factories.TryGetValue(nodeType, out Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode>? factory) || factory is null)
        {
            Type genericNodeType = typeof(SchemaNode<>).MakeGenericType(nodeType);
            ISchemaCoordinate2 coordinate = database.CalculateCoordinate(parent?.Coordinate, node);
            return (TDefinition)Activator.CreateInstance(genericNodeType, parent, coordinate, node)!;
        }

        return (TDefinition)factory.Invoke(database.CalculateCoordinate, parent, node);
    }

    private static Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode> FactoryBuilder<TNode, TDefinition>(
        Func<CoordinateFactory, ISchemaNode?, TNode, TDefinition> builder)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode
    {
        return (factory, parent, source) => builder.Invoke(factory, parent, (TNode)source);
    }
}
