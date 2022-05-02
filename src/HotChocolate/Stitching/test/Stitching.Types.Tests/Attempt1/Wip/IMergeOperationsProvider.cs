using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface IMergeOperationsProvider
{
    void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source,
        TParentTargetNode destination,
        Func<TParentSourceNode, TSourceSyntaxNode> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode;

    void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source,
        TParentTargetNode destination,
        Func<TParentSourceNode, IEnumerable<TSourceSyntaxNode>> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode;
}
