using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public class MergeOperationContext : OperationContextBase
{
    public IMergeOperationsProvider OperationsProvider { get; }

    public MergeOperationContext(IMergeOperationsProvider operationsProvider)
    {
        OperationsProvider = operationsProvider;
    }

    public void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source,
        TParentTargetNode destination,
        Func<TParentSourceNode, TSourceSyntaxNode> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode
    {
        OperationsProvider.Apply(
            source,
            destination,
            sourceAccessor,
            context);
    }

    public void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source,
        TParentTargetNode destination,
        Func<TParentSourceNode, IEnumerable<TSourceSyntaxNode>> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode
    {
        OperationsProvider.Apply(
            source,
            destination,
            sourceAccessor,
            context);
    }
}
