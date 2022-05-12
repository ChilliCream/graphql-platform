using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Language.Rewriters.Contracts;
using HotChocolate.Stitching.Types.Directives;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Rewriters;

public class RenameTypes<TContext> : Language.Rewriters.SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, ISyntaxReference> _renames = new();

    public RenameTypes(IList<ISyntaxReference> renames)
    {
        foreach (ISyntaxReference rename in renames)
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
        ISyntaxNavigator navigator,
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
        ISyntaxNavigator navigator,
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
        ISyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : ITypeDefinitionNode
    {
        if (!_renames.TryGetValue(name, out ISyntaxReference? syntaxReference))
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
        ISyntaxReference match,
        ISyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        if (!RenameDirective.TryParse(match.Node, out RenameDirective? renameDirective))
        {
            return node;
        }

        SchemaCoordinate coordinate = navigator.CreateCoordinate();
        TParent renamedNode = rewriteName.Invoke(node, renameDirective.NewName);

        IReadOnlyList<DirectiveNode> directives = node.Directives
            .ReplaceOrAddDirective(renameDirective.Directive, SourceDirective.Create(coordinate));

        return rewriteDirectives.Invoke(renamedNode, directives);
    }

    protected override NamedTypeNode RewriteNamedType(NamedTypeNode node, ISyntaxNavigator navigator, TContext context)
    {
        node = base.RewriteNamedType(node, navigator, context);

        if (!_renames.TryGetValue(node.Name, out ISyntaxReference? match))
        {
            return node;
        }

        if (!RenameDirective.TryParse(match.Node, out RenameDirective? renameDirective))
        {
            return node;
        }

        return node.WithName(new NameNode(renameDirective.NewName.Value));
    }
}
