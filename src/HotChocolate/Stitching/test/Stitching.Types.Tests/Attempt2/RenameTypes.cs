using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class RenameTypes<TContext> : SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, SyntaxReference> _renames = new();

    public RenameTypes(IList<SyntaxReference> renames)
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

    protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        node = base.RewriteInterfaceTypeDefinition(node, navigator, context);

        return RewriteType(
            node,
            node.Name,
            navigator,
            (_, name) => _.WithName(name),
            (_, directives) => _.WithDirectives(directives));
    }

    protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(ObjectTypeDefinitionNode node,
        SyntaxNavigator navigator,
        TContext context)
    {
        node = base.RewriteObjectTypeDefinition(node, navigator, context);

        return RewriteType(
            node,
            node.Name,
            navigator,
            (_, name) => _.WithName(name),
            (_, directives) => _.WithDirectives(directives));
    }

    private TParent RewriteType<TParent>(
        TParent node,
        NameNode name,
        SyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : ITypeDefinitionNode
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

    protected override NamedTypeNode RewriteNamedType(NamedTypeNode node, SyntaxNavigator navigator, TContext context)
    {
        node = base.RewriteNamedType(node, navigator, context);

        if (!_renames.TryGetValue(node.Name, out SyntaxReference match))
        {
            return node;
        }

        var renameDirective = new RenameDirective(match.Node as DirectiveNode);
        return node.WithName(new NameNode(renameDirective.NewName.Value));
    }
}