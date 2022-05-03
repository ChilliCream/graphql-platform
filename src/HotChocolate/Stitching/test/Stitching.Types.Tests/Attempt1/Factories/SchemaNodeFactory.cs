using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;

namespace HotChocolate.Stitching.Types;

internal static class SchemaNodeFactory
{
    private static readonly Dictionary<Type, NodeFactoryDelegate<ISyntaxNode, ISchemaNode>?> _factories = new()
    {
        {
            typeof(DocumentNode), FactoryBuilder<DocumentNode, DocumentDefinition>(
                (database, parent, node) =>
                    new DocumentDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        node))
        },
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (database, parent, node) =>
                    new ObjectTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (database, parent, node) =>
                    new ObjectTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
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
                (database, parent, node) =>
                    new InterfaceTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>(
                (database, parent, node) =>
                    new FieldDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (ITypeDefinition)parent!,
                        node))
        },
    };

    private static readonly Dictionary<Type, NodeFactoryDelegate<ISyntaxNode, ISchemaNode>?> _emptyFactories = new()
    {
        {
            typeof(DocumentNode), FactoryBuilder<DocumentNode, DocumentDefinition>(
                (database, parent, node) =>
                    new DocumentDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        new DocumentNode(new List<IDefinitionNode>(0))))
        },
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (database, parent, node) =>
                    new ObjectTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            node.Description,
                            new List<DirectiveNode>(0),
                            new List<NamedTypeNode>(0),
                            new List<FieldDefinitionNode>(0))))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (database, parent, node) =>
                    new ObjectTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            default,
                            new List<DirectiveNode>(0),
                            new List<NamedTypeNode>(0),
                            new List<FieldDefinitionNode>(0))))
        },
        {
            typeof(InterfaceTypeDefinitionNode), FactoryBuilder<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>(
                (database, parent, node) =>
                    new InterfaceTypeDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (DocumentDefinition)parent!,
                        new InterfaceTypeDefinitionNode(
                            default,
                            node.Name,
                            node.Description,
                            new List<DirectiveNode>(0),
                            new List<NamedTypeNode>(0),
                            new List<FieldDefinitionNode>(0))))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>(
                (database, parent, node) =>
                    new FieldDefinition(
                        database,
                        database.CalculateCoordinate(parent?.Coordinate, node),
                        (ITypeDefinition)parent!,
                        new FieldDefinitionNode(
                            default,
                            node.Name,
                            node.Description,
                            new List<InputValueDefinitionNode>(0),
                            node.Type,
                            new List<DirectiveNode>(0))))
        },
    };

    public static DocumentDefinition CreateNewDocument(ISchemaDatabase schemaDatabase)
    {
        var documentNode = new DocumentNode(new List<IDefinitionNode>(0));
        return CreateDocument(schemaDatabase, documentNode);
    }

    public static DocumentDefinition CreateDocument(ISchemaDatabase database, DocumentNode node)
    {
        ISchemaCoordinate2 coordinate = database.CalculateCoordinate(default, node);
        var definition = new DocumentDefinition(
            database,
            coordinate,
            node);

        database.Reindex(definition);

        return definition;
    }

    public static ISchemaNode Create(
        ISchemaDatabase database,
        ISchemaNode? parent,
        ISyntaxNode node)
    {
        Type nodeType = node.GetType();

        if (!_factories.TryGetValue(nodeType, out NodeFactoryDelegate<ISyntaxNode, ISchemaNode>? factory) || factory is null)
        {
            Type genericNodeType = typeof(SchemaNode<>).MakeGenericType(nodeType);
            ISchemaCoordinate2 coordinate = database.CalculateCoordinate(parent?.Coordinate, node);
            return (ISchemaNode)Activator.CreateInstance(genericNodeType, database, coordinate, parent, node)!;
        }

        return factory.Invoke(database, parent, node);
    }

    private static NodeFactoryDelegate<ISyntaxNode, ISchemaNode> FactoryBuilder<TNode, TDefinition>(
        NodeFactoryDelegate<TNode, TDefinition> builder)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode
    {
        return (database, parent, source) =>
            builder.Invoke(database, parent, (TNode)source);
    }

    private delegate TDefinition NodeFactoryDelegate<in TNode,
        out TDefinition>(
        ISchemaDatabase database,
        ISchemaNode? parent,
        TNode node)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode;

    public static ISchemaNode CreateEmpty(SchemaDatabase database, ISchemaNode? parent, ISyntaxNode node)
    {
        Type nodeType = node.GetType();

        if (!_emptyFactories.TryGetValue(nodeType, out NodeFactoryDelegate<ISyntaxNode, ISchemaNode>? factory) || factory is null)
        {
            Type genericNodeType = typeof(SchemaNode<>).MakeGenericType(nodeType);
            ISchemaCoordinate2 coordinate = database.CalculateCoordinate(parent?.Coordinate, node);
            return (ISchemaNode)Activator.CreateInstance(genericNodeType, database, coordinate, parent, node)!;
        }

        return factory.Invoke(database, parent, node);
    }
}
