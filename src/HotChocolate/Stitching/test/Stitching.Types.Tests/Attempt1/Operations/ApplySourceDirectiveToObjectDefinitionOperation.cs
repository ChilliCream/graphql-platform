using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToObjectDefinitionOperation<TSyntaxNode, TSchemaNode> : ApplySourceDirectiveOperationBase, IMergeSchemaNodeOperation<TSyntaxNode, TSchemaNode>
    where TSyntaxNode : IHasDirectives, IHasWithDirectives<TSyntaxNode>, ISyntaxNode
    where TSchemaNode : ISchemaNode<TSyntaxNode>
{
    public void Apply(TSyntaxNode source, TSchemaNode target, MergeOperationContext context)
    {
        ApplySourceDirective<TSyntaxNode, TSchemaNode>(target, context);
    }
}
