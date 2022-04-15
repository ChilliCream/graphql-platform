using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        DocumentNode source = Utf8GraphQLParser.Parse(@"
            interface TestInterface {
              foo: Test2!
            }
        ");

        DocumentNode source1 = Utf8GraphQLParser.Parse(@"
            interface TestInterface @rename(name: ""TestInterface_renamed"") {
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
        DefaultSyntaxNodeVisitor visitor = new DefaultSyntaxNodeVisitor(coordinateProvider, schemaNodeFactory, operationProvider);

        var documentNode = new DocumentNode(new List<IDefinitionNode>(0));
        var documentDefinition = new DocumentDefinition(coordinateProvider.Add, documentNode);

        //visitor.Accept(source);
        visitor.Accept(source1);
        visitor.Accept(source2);

        var operations = new List<ISchemaNodeRewriteOperation> { new RenameOperation() };
        var schemaOperations = new SchemaOperations(operations, coordinateProvider);
        schemaOperations.Apply(documentDefinition);

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

internal class SchemaOperations
{
    private readonly List<ISchemaNodeRewriteOperation> _operations;
    private readonly ISchemaCoordinate2Provider _coordinateProvider;

    public SchemaOperations(List<ISchemaNodeRewriteOperation> operations, ISchemaCoordinate2Provider coordinateProvider)
    {
        _operations = operations;
        _coordinateProvider = coordinateProvider;
    }

    public void Apply(DocumentDefinition documentDefinition)
    {
        IEnumerable<ISchemaNode?> nodes = documentDefinition
            .DescendentNodes(_coordinateProvider);

        foreach (ISchemaNode? node in nodes)
        {
            if (node.Definition is DirectiveNode)
            {

            }
            foreach (ISchemaNodeRewriteOperation operation in _operations)
            {
                if (!operation.CanHandle(node))
                {
                    continue;
                }

                operation.Handle(node);
            }
        }
    }
}

internal class FactoryContext
{
    public CoordinateProvider Provider { get; }

    public FactoryContext(CoordinateProvider coordinateProvider)
    {
        Provider = coordinateProvider;
    }

    public ISchemaCoordinate2 CreateCoordinate(ISchemaNode node)
    {
        return Provider.Add(node);
    }

    public T? GetParent<T>(ISyntaxNode node)
    {
        ISchemaCoordinate2 coordinate = Provider.Get(node) ?? throw new InvalidOperationException();

        if (coordinate.Parent is null
            || !Provider.TryGet(coordinate.Parent, out ISchemaNode? parentDefinition)
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

    private readonly Dictionary<Type, Func<FactoryContext, ISchemaNode?, ISyntaxNode, ISchemaNode>?> _factories = new()
    {
        {
            typeof(DocumentNode), FactoryBuilder<DocumentNode, DocumentDefinition>(
                (ctx, _, node) =>
                    new DocumentDefinition(
                        ctx.CreateCoordinate,
                        new DocumentNode(default,
                            node.Definitions)))
        },
        {
            typeof(ObjectTypeDefinitionNode), FactoryBuilder<ObjectTypeDefinitionNode, ObjectTypeDefinition>(
                (ctx, parent, node) =>
                    new ObjectTypeDefinition(
                        ctx.CreateCoordinate,
                        (DocumentDefinition)parent!,
                        new ObjectTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(ObjectTypeExtensionNode), FactoryBuilder<ObjectTypeExtensionNode, ObjectTypeDefinition>(
                (ctx, parent, node) =>
                    new ObjectTypeDefinition(
                        ctx.CreateCoordinate,
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
                (ctx, parent, node) =>
                    new InterfaceTypeDefinition(
                        ctx.CreateCoordinate,
                        (DocumentDefinition)parent!,
                        new InterfaceTypeDefinitionNode(default,
                            node.Name,
                            default,
                            node.Directives,
                            node.Interfaces,
                            node.Fields)))
        },
        {
            typeof(FieldDefinitionNode), FactoryBuilder<FieldDefinitionNode, FieldDefinition>(
                (ctx, parent, node) =>
                    new FieldDefinition(
                        ctx.CreateCoordinate,
                        (ITypeDefinition)parent!,
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

    public ISchemaNode Create<TNode>(TNode node)
        where TNode : ISyntaxNode
    {
        return AddOrGet(default, node);
    }

    public bool TryAddOrGet(ISyntaxNode? parent, ISyntaxNode node, out ISchemaNode schemaNode)
    {
        if (parent is null)
        {
            schemaNode = AddOrGet(default, node);
            return true;
        }

        if (!_context.Provider.TryGet(parent, out ISchemaNode? parentNode))
        {
            throw new InvalidOperationException();
        }

        schemaNode = AddOrGet(parentNode, node);
        return true;
    }

    public ISchemaNode AddOrGet(ISchemaNode? parent, ISyntaxNode node)
    {
        ISchemaCoordinate2 coordinate = _context.Provider.CalculateCoordinate(parent, node);

        if (_context.Provider.TryGet(coordinate, out ISchemaNode? existingNode))
        {
            return existingNode;
        }

        Type nodeType = node.GetType();
        if (!_factories.TryGetValue(nodeType, out Func<FactoryContext, ISchemaNode?, ISyntaxNode, ISchemaNode>? factory) || factory is null)
        {
            Type genericNodeType = typeof(SchemaNode<>).MakeGenericType(nodeType);
            return (ISchemaNode)Activator.CreateInstance(genericNodeType, _context.CreateCoordinate, parent, node)!;
        }

        return factory.Invoke(_context, parent, node);
    }

    private static Func<FactoryContext, ISchemaNode?, ISyntaxNode, ISchemaNode> FactoryBuilder<TNode, TDefinition>(
        Func<FactoryContext, ISchemaNode?, TNode, TDefinition> builder)
        where TNode : ISyntaxNode
        where TDefinition : ISchemaNode
    {
        return (ctx, parent, source) => builder.Invoke(ctx, parent, (TNode) source);
    }
}
