using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class DefaultSyntaxNodeVisitor : SyntaxNodeVisitor
{
    private readonly CoordinateProvider _provider;
    private readonly SchemaNodeFactory _schemaNodeFactory;
    private readonly DefaultOperationProvider _operationProvider;

    public DefaultSyntaxNodeVisitor(
        CoordinateProvider provider,
        SchemaNodeFactory schemaNodeFactory,
        DefaultOperationProvider operationProvider)
        : base(VisitorAction.Continue)
    {
        _provider = provider;
        _schemaNodeFactory = schemaNodeFactory;
        _operationProvider = operationProvider;

        _visitorConfiguration = new VisitorExtensions.VisitorConfiguration(Enter, Leave);
    }

    private readonly VisitorExtensions.VisitorConfiguration _visitorConfiguration;

    public void Accept(DocumentNode source)
    {
        source.Accept(this, _visitorConfiguration);
    }

    private readonly Stack<ISchemaCoordinate2> _coordinates = new();

    private bool Enter(Type type, out SyntaxNodeVisitorProvider.IntVisitorFn? fn)
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
            var added = _schemaNodeFactory.TryAddOrGet(parent, node, out ISchemaNode schemaNode);
            if (added)
            {
                schemaNode.RewriteDefinition(schemaNode.Definition);

                if (schemaNode?.Definition is IHasName nameNode &&
                    nameNode.Name.Value == "Test")
                {

                }
            }
            else
            {
                if (schemaNode?.Definition is IHasName nameNode &&
                    nameNode.Name.Value == "Test")
                {

                }

                schemaNode?.Apply(node, _operationProvider);
            }

            if (schemaNode is not null)
            {
                _coordinates.Push(schemaNode.Coordinate);
            }

            return enterFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
        };

        return true;
    }

    private bool Leave(Type type, out SyntaxNodeVisitorProvider.IntVisitorFn? fn)
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
            _coordinates.Pop();
            return leaveFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
        };

        return true;
    }
}
