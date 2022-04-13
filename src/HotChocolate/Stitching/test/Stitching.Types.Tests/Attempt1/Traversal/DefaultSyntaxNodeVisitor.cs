using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class DefaultSyntaxNodeVisitor : SyntaxNodeVisitor
{
    private readonly CoordinateProvider _provider;

    public DefaultSyntaxNodeVisitor(CoordinateProvider provider)
        : base(VisitorAction.Continue)
    {
        _provider = provider;
    }

    private readonly IList<ISchemaNodeRewriteOperation> _rewriters =
        new List<ISchemaNodeRewriteOperation>() { new RenameOperation() };

    private readonly IList<(ISchemaCoordinate2 Coordinate, ISchemaNodeRewriteOperation Operation)>
        _operations =
            new List<(ISchemaCoordinate2 Coordinate, ISchemaNodeRewriteOperation Operation)>();

    public IReadOnlyList<(ISchemaCoordinate2 Coordinate, ISchemaNodeRewriteOperation Operation)>
        RewriteOperations => new ReadOnlyCollection<(ISchemaCoordinate2 Coordinate, ISchemaNodeRewriteOperation Operation)>(_operations);

    public override VisitorAction Enter(DirectiveNode node, ISyntaxNode parent, IReadOnlyList<object> path, IReadOnlyList<ISyntaxNode> ancestors)
    {
        ISchemaCoordinate2? nodeCoordinate = _provider.Get(node);
        if (nodeCoordinate?.Parent == null)
        {
            return base.Enter(node, parent, path, ancestors);
        }

        foreach (ISchemaNodeRewriteOperation rewriter in _rewriters)
        {
            if (!nodeCoordinate.IsMatch(rewriter.Match))
            {
                continue;
            }

            _operations.Add((nodeCoordinate.Parent, rewriter));
        }

        return base.Enter(node, parent, path, ancestors);
    }
}
