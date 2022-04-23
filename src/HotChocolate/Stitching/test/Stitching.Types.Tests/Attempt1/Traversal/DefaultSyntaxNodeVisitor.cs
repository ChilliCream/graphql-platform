using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Traversal;

internal class DefaultSyntaxNodeVisitor : SyntaxNodeVisitor
{
    private readonly ISchemaDatabase _schemaDatabase;
    private readonly DefaultMergeOperationsProvider _operationsProvider;

    public DefaultSyntaxNodeVisitor(
        ISchemaDatabase schemaDatabase,
        DefaultMergeOperationsProvider operationsProvider)
        : base(VisitorAction.Continue)
    {
        _schemaDatabase = schemaDatabase;
        _operationsProvider = operationsProvider;

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

            var existingNode = _schemaDatabase.TryGet(parent, node, out ISchemaNode? schemaNode);
            if (!existingNode)
            {
                schemaNode = _schemaDatabase.Reindex(parent, node);
                schemaNode.RewriteDefinition(schemaNode, schemaNode.Definition);
            }
            else
            {
                var operationContext = new MergeOperationContext(_schemaDatabase, _operationsProvider);
                schemaNode.Apply(node, operationContext);
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
