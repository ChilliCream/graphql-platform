using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters.Contracts;

namespace HotChocolate.Stitching.Types;

public class IgnoreNode<TContext> : Language.Rewriters.SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, ISyntaxReference> _ignores = new();

    public IgnoreNode(IList<ISyntaxReference> ignoredNodes)
    {
        foreach (ISyntaxReference ignoredNode in ignoredNodes)
        {
            if (ignoredNode.Parent?.Node is not INamedSyntaxNode namedSyntaxNode)
            {
                throw new NotSupportedException();
            }

            NameNode oldName = namedSyntaxNode.Name;
            _ignores.Add(oldName, ignoredNode);
        }
    }

    protected override ITypeSystemDefinitionNode RewriteTypeDefinition(
        ITypeSystemDefinitionNode node,
        ISyntaxNavigator navigator,
        TContext context)
    {
        if (node is not INamedSyntaxNode namedSyntaxNode)
        {
            return node;
        }

        NameNode name = namedSyntaxNode.Name;
        if (!_ignores.TryGetValue(name, out ISyntaxReference syntaxReferences))
        {
            return node;
        }

        ITypeDefinitionNode? typeDefinitionNode = navigator.GetAncestor<ITypeDefinitionNode>();

        return base.RewriteTypeDefinition(node, navigator, context);
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(FieldDefinitionNode node, ISyntaxNavigator navigator, TContext context)
    {
        return base.RewriteFieldDefinition(node, navigator, context);
    }
}
