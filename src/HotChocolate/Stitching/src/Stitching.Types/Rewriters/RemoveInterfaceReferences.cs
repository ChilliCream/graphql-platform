using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters.Contracts;

namespace HotChocolate.Stitching.Types.Rewriters;

public class RemoveInterfaceReferences<TContext> : Language.Rewriters.SchemaSyntaxRewriter<TContext>
{
    private readonly HashSet<NameNode> _interfaces;

    public RemoveInterfaceReferences(IReadOnlyList<NameNode> interfaces)
    {
        _interfaces = new HashSet<NameNode>(interfaces, SyntaxComparer.BySyntax);
    }

    protected override DocumentNode RewriteDocument(DocumentNode node, ISyntaxNavigator navigator, TContext context)
    {
        node = node.WithDefinitions(new List<IDefinitionNode>(node.Definitions.Where(x => x is not null)));
        node = base.RewriteDocument(node, navigator, context);
        return node;
    }

    protected override ITypeSystemDefinitionNode RewriteTypeDefinition(
        ITypeSystemDefinitionNode node,
        ISyntaxNavigator navigator,
        TContext context)
    {
        switch (node)
        {
            case ObjectTypeDefinitionNode objectTypeDefinition:
                objectTypeDefinition = objectTypeDefinition.WithFields(
                    new List<FieldDefinitionNode>(objectTypeDefinition.Fields.Where(x
                        => x is not null)));

                if (objectTypeDefinition.Fields.Count == 0)
                {
                    return default!;
                }

                objectTypeDefinition = objectTypeDefinition.WithInterfaces(
                    new List<NamedTypeNode>(objectTypeDefinition.Interfaces
                        .Where(x => !_interfaces.Contains(x.Name))));

                node = objectTypeDefinition;
                break;
        }

        node = base.RewriteTypeDefinition(node, navigator, context);
        return node;
    }
}
