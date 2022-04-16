using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Traversal;

internal class DefaultSyntaxNodeVisitor : SyntaxNodeVisitor
{
    private readonly ISchemaDatabase _provider;
    private readonly SchemaNodeFactory _schemaNodeFactory;
    private readonly DefaultOperationProvider _operationProvider;

    public DefaultSyntaxNodeVisitor(
        ISchemaDatabase provider,
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
            if (ancestors.Count > 1)
            {
                return VisitorAction.Skip;
            }

            var existingNode = _provider.TryGet(parent, node, out ISchemaNode? schemaNode);
            if (!existingNode)
            {
                schemaNode = _provider.Reindex(parent, node);
            }
            else
            {
                schemaNode.Apply(node, _operationProvider);
            }

            _coordinates.Push(schemaNode.Coordinate);

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
            if (ancestors.Count > 1)
            {
                return VisitorAction.Skip;
            }

            _coordinates.Pop();

            return leaveFn!.Invoke(nodeVisitor, node, parent, path, ancestors);
        };

        return true;
    }
}
