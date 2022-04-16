using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class SchemaNodeFactory
{
    private readonly Dictionary<Type, Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode>?> _factories = new()
    {
        {
            typeof(DocumentNode), FactoryBuilder<DocumentNode, DocumentDefinition>(
                (coordinateFactory, _, node) =>
                    new DocumentDefinition(
                        coordinateFactory,
                        node))
        },
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (coordinateFactory, parent, node) =>
                    new ObjectTypeDefinition(
                        coordinateFactory,
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (coordinateFactory, parent, node) =>
                    new ObjectTypeDefinition(
                        coordinateFactory,
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
                        coordinateFactory,
                        (DocumentDefinition)parent!,
                        node))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>(
                (coordinateFactory, parent, node) =>
                    new FieldDefinition(
                        coordinateFactory,
                        (ITypeDefinition)parent!,
                        node))
        },
    };

    public bool Create(ISchemaNode? parent, ISyntaxNode node, CoordinateFactory coordinateFactory, out ISchemaNode schemaNode)
    {
        Type nodeType = node.GetType();

        if (!_factories.TryGetValue(nodeType, out Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode>? factory) || factory is null)
        {
            Type genericNodeType = typeof(SchemaNode<>).MakeGenericType(nodeType);
            schemaNode = (ISchemaNode)Activator.CreateInstance(genericNodeType, coordinateFactory, parent, node)!;
        }
        else
        {
            schemaNode = factory.Invoke(coordinateFactory, parent, node);
        }

        return true;
    }

    private static Func<CoordinateFactory, ISchemaNode?, ISyntaxNode, ISchemaNode> FactoryBuilder<TNode, TDefinition>(
        Func<CoordinateFactory, ISchemaNode?, TNode, TDefinition> builder)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode
    {
        return (factory, parent, source) => builder.Invoke(factory, parent, (TNode)source);
    }
}
