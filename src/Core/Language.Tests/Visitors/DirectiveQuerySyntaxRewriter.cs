using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class DirectiveQuerySyntaxRewriter
        : QuerySyntaxRewriter<DirectiveNode>
    {
        protected override FieldNode RewriteField(
            FieldNode node,
            DirectiveNode directive)
        {
            var directives = new List<DirectiveNode>(node.Directives);
            directives.Add(directive);

            FieldNode rewritten = node.WithDirectives(directives);

            return base.RewriteField(rewritten, directive);
        }
    }
}
