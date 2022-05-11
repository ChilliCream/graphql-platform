using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class RenameFields<TContext> : SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, SyntaxReference> _renames = new();

    public RenameFields(IList<SyntaxReference> renames)
    {
        foreach (SyntaxReference rename in renames)
        {
            if (rename.Parent?.Node is not INamedSyntaxNode namedSyntaxNode)
            {
                throw new NotSupportedException();
            }

            NameNode oldName = namedSyntaxNode.Name;
            _renames.Add(oldName, rename);
        }
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(FieldDefinitionNode node, SyntaxNavigator navigator,
        TContext context)
    {
        node = base.RewriteFieldDefinition(node, navigator, context);

        return RewriteField(
            node,
            node.Name,
            navigator,
            (_, name) => _.WithName(name),
            (_, directives) => _.WithDirectives(directives));
    }

    private TParent RewriteField<TParent>(
        TParent node,
        NameNode name,
        SyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        if (!_renames.TryGetValue(name, out SyntaxReference syntaxReference))
        {
            return node;
        }

        return Rewrite(node,
            syntaxReference,
            navigator,
            rewriteName,
            rewriteDirectives);
    }

    private static TParent Rewrite<TParent>(TParent node,
        SyntaxReference match,
        SyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        var directiveNode = match.Node as DirectiveNode;
        var renameDirective = new RenameDirective(directiveNode);
        SchemaCoordinate coordinate = navigator.CreateCoordinate();
        TParent renamedNode = rewriteName.Invoke(node, new NameNode(renameDirective.NewName.Value));

        var directives = node.Directives
            .Except<DirectiveNode>(new[] { directiveNode }, SyntaxComparer.BySyntax)
            .Concat(new[]
            {
                new DirectiveNode("source",
                    new ArgumentNode("coordinate", coordinate.ToString()))
            })
            .ToList();

        return rewriteDirectives.Invoke(renamedNode, directives);
    }
}