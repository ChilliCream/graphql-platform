using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class BasicDocumentMergeTests
{
    [Fact]
    public void Test()
    {
        DocumentNode source1 = Utf8GraphQLParser.Parse(@"
            interface TestInterface {
              foo: Test2!
            }

            extend type Test implements TestInterface {
              foo: Test2! @rename(name: ""foo_renamed"")
            }
            ",
            ParserOptions.NoLocation);

        DocumentNode source2 = Utf8GraphQLParser.Parse(@"
            type Test @rename(name: ""test_renamed"") {
              id: String! @rename(name: ""id_renamed"")
              foo: String!
            }

            type Test2 {
              test: Test!
            }
            ",
            ParserOptions.NoLocation);

        DefaultOperationProvider operationProvider = new DefaultOperationProvider();
        CoordinateProvider coordinateProvider = new CoordinateProvider();
        FactoryContext factoryContext = new FactoryContext(coordinateProvider);
        SchemaNodeFactory schemaNodeFactory = new SchemaNodeFactory(factoryContext);
        DefaultSyntaxNodeVisitor visitor = new DefaultSyntaxNodeVisitor(coordinateProvider);

        var documentNode = new DocumentNode(new List<IDefinitionNode>(0));
        var documentDefinition = new DocumentDefinition(documentNode);
        ISchemaCoordinate2 documentNodeCoordinate = coordinateProvider.Add(documentNode);
        coordinateProvider.Associate(documentNodeCoordinate, documentDefinition);

        bool Enter(Type type, out SyntaxNodeVisitorProvider.IntVisitorFn? fn)
        {
            fn = default;
            var baseVisitor =
                SyntaxNodeVisitorProvider.GetEnterVisitor(type, out SyntaxNodeVisitorProvider.IntVisitorFn? enterFn)
                && enterFn is not null;

            if (!baseVisitor)
            {
                return false;
            }

            fn = (nodeVisitor, node, parent, path, ancestors) =>
            {
                if (node is DocumentNode)
                {
                    return enterFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
                }

                ISchemaCoordinate2 coordinate = coordinateProvider.Add(node);
                if (!coordinateProvider.TryGet(coordinate, out ISchemaNode? existingNode))
                {
                    existingNode = schemaNodeFactory.Create(node);
                }

                if (existingNode is not null)
                {
                    existingNode.RewriteDefinition(existingNode.Definition);
                    coordinateProvider.Associate(coordinate, existingNode);
                    existingNode.Apply(node, operationProvider);
                }

                return enterFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
            };

            return true;
        }

        bool Leave(Type type, out SyntaxNodeVisitorProvider.IntVisitorFn? fn)
        {
            fn = default;
            var baseVisitor =
                SyntaxNodeVisitorProvider.GetLeaveVisitor(type, out SyntaxNodeVisitorProvider.IntVisitorFn? leaveFn)
                && leaveFn is not null;

            if (!baseVisitor)
            {
                return false;
            }

            fn = (nodeVisitor, node, parent, path, ancestors) =>
            {
                if (node is DocumentNode)
                {
                    return leaveFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
                }

                coordinateProvider.Remove();
                return leaveFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
            };

            return true;
        }

        var visitorConfiguration = new VisitorExtensions.VisitorConfiguration(Enter, Leave);
        source1.Accept(visitor, visitorConfiguration);
        source2.Accept(visitor, visitorConfiguration);

        var context = new OperationContext(operationProvider);
        foreach (var operation in visitor.RewriteOperations)
        {
            if (!coordinateProvider.TryGet(operation.Coordinate, out ISchemaNode? targetNode))
            {
                continue;
            }

            ISyntaxNode result = operation.Operation.Apply(targetNode.Definition, operation.Coordinate, context);
            ISchemaCoordinate2? coordinate = operation.Coordinate;

            while (coordinate is not null)
            {
                if (!coordinateProvider.TryGet(coordinate, out ISchemaNode? schemaNode))
                {
                    coordinate = coordinate.Parent;
                    continue;
                }

                schemaNode.RewriteDefinition(result);
                break;
            }
        }

        ISchemaNode renderedSchema = coordinateProvider.Root;
        var schema = renderedSchema.Definition.Print();
        schema.MatchSnapshot();

        //var serviceReference = new HttpService("Test", "https://localhost", new[] { new AllowBatchingFeature() });
        //var definition = new ServiceDefinition(serviceReference,
        //    new List<DocumentNode> { new(new List<IDefinitionNode>(0)) });

        //var transformer = new SchemaTransformer();
        //var result = await transformer.Transform(definition, new SchemaTransformationOptions());
        //var subGraph = result.SubGraph;
    }
}

internal class FactoryContext
{
    private readonly CoordinateProvider _coordinateProvider;

    public FactoryContext(CoordinateProvider coordinateProvider)
    {
        _coordinateProvider = coordinateProvider;
    }

    public T? GetParent<T>(ISyntaxNode node)
    {
        ISchemaCoordinate2 coordinate = _coordinateProvider.Get(node) ?? throw new InvalidOperationException();

        if (coordinate.Parent is null
            || !_coordinateProvider.TryGet(coordinate.Parent, out ISchemaNode? parentDefinition)
            || parentDefinition is not T typedParent)
        {
            return default;
        }

        return typedParent;
    }
}

internal class SchemaNodeFactory
{
    private readonly FactoryContext _context;

    private readonly Dictionary<Type, Func<FactoryContext, object, ISchemaNode>> _factories = new()
    {
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (ctx, node) =>
                    new ObjectTypeDefinition(
                        ctx.GetParent<DocumentDefinition>(node)!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (ctx, node) =>
                    new ObjectTypeDefinition(
                        ctx.GetParent<DocumentDefinition>(node)!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(InterfaceTypeDefinitionNode), FactoryBuilder<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>(
                (ctx, node) =>
                    new InterfaceTypeDefinition(
                        ctx.GetParent<DocumentDefinition>(node)!,
                        new InterfaceTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>((ctx, node) =>
                new FieldDefinition(
                    ctx.GetParent<ITypeDefinition>(node)!,
                    new FieldDefinitionNode(default,
                        node.Name,
                        default,
                        node.Arguments,
                        node.Type,
                        node.Directives)))
        },
    };

    public SchemaNodeFactory(FactoryContext context)
    {
        _context = context;
    }

    public ISchemaNode? Create<TNode>(TNode node)
        where TNode : ISyntaxNode
    {
         if (!_factories.TryGetValue(node.GetType(), out Func<FactoryContext, object, ISchemaNode>? factory))
         {
             return default;
         }

         return factory.Invoke(_context, node);
    }

    private static Func<FactoryContext, object, ISchemaNode> FactoryBuilder<TNode, TDefinition>(Func<FactoryContext, TNode, TDefinition> builder)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode
    {
        return (ctx, source) => builder.Invoke(ctx, (TNode) source);
    }
}
