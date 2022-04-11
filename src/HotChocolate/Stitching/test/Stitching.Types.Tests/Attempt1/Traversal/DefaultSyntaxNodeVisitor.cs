using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class DefaultSyntaxNodeVisitor : SyntaxNodeVisitor
{
    private readonly DocumentDefinition _target;
    private readonly IOperationProvider _operationProvider;
    private readonly CoordinateProvider _coordinateProvider = new();

    public DefaultSyntaxNodeVisitor(DocumentDefinition target, IOperationProvider operationProvider)
        : base(VisitorAction.Continue)
    {
        _target = target;
        _operationProvider = operationProvider;
    }

    public override VisitorAction Enter(DocumentNode node, ISyntaxNode parent, IReadOnlyList<object> path, IReadOnlyList<ISyntaxNode> ancestors)
    {
        // TODO: Move this to a base enter/leave construct for all node types - similar to path.
        ISchemaCoordinate2 _ = _coordinateProvider.Add(node);

        return base.Enter(node, parent, path, ancestors);
    }

    public override VisitorAction Leave(DocumentNode node, ISyntaxNode parent, IReadOnlyList<object> path, IReadOnlyList<ISyntaxNode> ancestors)
    {
        // TODO: Move this to a base enter/leave construct for all node types - similar to path.
        _coordinateProvider.Remove();
        return base.Leave(node, parent, path, ancestors);
    }

    public override VisitorAction Enter(ObjectTypeDefinitionNode node, ISyntaxNode parent, IReadOnlyList<object> path, IReadOnlyList<ISyntaxNode> ancestors)
    {
        ISchemaCoordinate2 nodeCoordinate = _coordinateProvider.Add(node);

        // TODO: Look at better factory approach for creating new Definition nodes
        ObjectTypeDefinition destination = GetOrAdd(nodeCoordinate,
            () =>
            {
                var definition = new ObjectTypeDefinition(
                    new ObjectTypeDefinitionNode(default,
                        node.Name,
                        node.Description,
                        node.Directives,
                        node.Interfaces,
                        node.Fields));

                _target.Add(definition);

                return definition;
            });


        // Apply the Schema operation rules for this node type.
        destination.Apply(node, _operationProvider);
        return base.Enter(node, parent, path, ancestors);
    }

    public override VisitorAction Leave(ObjectTypeDefinitionNode node, ISyntaxNode parent, IReadOnlyList<object> path,
        IReadOnlyList<ISyntaxNode> ancestors)
    {
        _coordinateProvider.Remove();
        return base.Leave(node, parent, path, ancestors);
    }

    public override VisitorAction Enter(ObjectTypeExtensionNode node, ISyntaxNode parent, IReadOnlyList<object> path, IReadOnlyList<ISyntaxNode> ancestors)
    {
        ISchemaCoordinate2 nodeCoordinate = _coordinateProvider.Add(node);

        ObjectTypeDefinition schemaNode = GetOrAdd(nodeCoordinate,
            () =>
            {
                var definition = new ObjectTypeDefinition(
                    new ObjectTypeDefinitionNode(default,
                        node.Name,
                        default,
                        node.Directives,
                        node.Interfaces,
                        node.Fields));

                _target.Add(definition);

                return definition;
            });

        // Apply the Schema operation rules for this node type.
        schemaNode.Apply(node, _operationProvider);
        return base.Enter(node, parent, path, ancestors);
    }

    public override VisitorAction Leave(ObjectTypeExtensionNode node, ISyntaxNode parent, IReadOnlyList<object> path,
        IReadOnlyList<ISyntaxNode> ancestors)
    {
        _coordinateProvider.Remove();
        return base.Leave(node, parent, path, ancestors);
    }

    private TDefinition GetOrAdd<TDefinition>(ISchemaCoordinate2 coordinate, Func<TDefinition> destinationFactory)
        where TDefinition : ISchemaNode
    {
        if (_coordinateProvider.TryGet(coordinate, out ISchemaNode? destination)
            && destination is TDefinition typedDestination)
        {
            return typedDestination;
        }

        typedDestination = destinationFactory();
        _coordinateProvider.Associate(coordinate, typedDestination);
        return typedDestination;
    }

    private TDefinition? Get<TDefinition>(SchemaCoordinate2 coordinate)
        where TDefinition : ISchemaNode
    {
        if (_coordinateProvider.TryGet(coordinate, out ISchemaNode? destination)
            && destination is TDefinition typedDefinition)
        {
            return typedDefinition;
        }

        return default;
    }
}
