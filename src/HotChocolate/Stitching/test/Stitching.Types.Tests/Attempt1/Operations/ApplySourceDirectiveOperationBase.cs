using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveOperationBase
{
    protected static void ApplySourceDirective<TSchemaNode, TSyntaxNode>(
        TSchemaNode source,
        TSchemaNode target,
        MergeOperationContext _)
        where TSchemaNode : ISchemaNode<TSyntaxNode>
        where TSyntaxNode : ISyntaxNode, IHasDirectives, IHasWithDirectives<TSyntaxNode>
    {
        var sourceName = source.Database.Name;
        if (sourceName is null)
        {
            return;
        }

        var sourceDirectiveNode = new DirectiveNode("_hc_source",
            new ArgumentNode("schema", sourceName));

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
