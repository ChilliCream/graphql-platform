using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveOperationBase
{
    protected static void ApplySourceDirective<TSyntaxNode, TSchemaNode>(
        TSchemaNode target,
        MergeOperationContext context)
        where TSyntaxNode : IHasDirectives, IHasWithDirectives<TSyntaxNode>, ISyntaxNode
        where TSchemaNode : ISchemaNode<TSyntaxNode>
    {
        NameNode? sourceName = context.Source;
        if (sourceName is null)
        {
            return;
        }

        var sourceDirectiveNode = new DirectiveNode("_hc_source",
            new ArgumentNode("schema", sourceName.Value));

        IReadOnlyList<DirectiveNode> updatedDirectives = target
            .Definition
            .Directives
            .AddOrReplace(sourceDirectiveNode,
                node => sourceDirectiveNode.IsEqualTo(node));

        TSyntaxNode fieldDefinitionNode = target.Definition
            .WithDirectives(updatedDirectives);

        target.RewriteDefinition(fieldDefinitionNode);
    }
}
