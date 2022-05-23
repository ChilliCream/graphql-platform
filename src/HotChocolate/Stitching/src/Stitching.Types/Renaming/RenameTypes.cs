using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Directives;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Renaming;

public class RenameTypes<TContext> : SchemaSyntaxRewriterWithNavigation<TContext>
    where TContext : IRewriteContext
{
    private readonly Dictionary<NameNode, SyntaxReference> _renames = new();

    public RenameTypes(IReadOnlyList<SyntaxReference> renames)
    {
        foreach (SyntaxReference rename in renames)
        {
            ISyntaxNode? parent = rename.Parent;
            if (parent is not INamedSyntaxNode namedSyntaxNode)
            {
                throw new NotSupportedException();
            }

            NameNode oldName = namedSyntaxNode.Name;
            _renames.Add(oldName, rename);
        }
    }

    protected override ITypeSystemDefinitionNode RewriteTypeDefinition(ITypeSystemDefinitionNode node, TContext context)
    {
        node = base.RewriteTypeDefinition(node, context);
        if (node is not INamedSyntaxNode namedSyntaxNode)
        {
            return node;
        }

        NameNode name = namedSyntaxNode.Name;
        if (!_renames.TryGetValue(name, out SyntaxReference? syntaxReference))
        {
            return node;
        }

        if (syntaxReference.Node is not RenameDirective renameDirective)
        {
            return node;
        }

        node = Rename(node, renameDirective, context);
        return node;
    }
    
    protected override NamedTypeNode RewriteNamedType(NamedTypeNode node, TContext context)
    {
        node = base.RewriteNamedType(node, context);

        if (!_renames.TryGetValue(node.Name, out SyntaxReference? match))
        {
            return node;
        }

        if (match.Node is not RenameDirective renameDirective)
        {
            return node;
        }

        return node.WithName(new NameNode(renameDirective.NewName.Value));
    }

    private static ITypeSystemDefinitionNode Rename(
        ITypeSystemDefinitionNode node,
        RenameDirective renameDirective,
        TContext context)
    {
        SchemaCoordinate coordinate = context.Navigator.CreateCoordinate(node);
        DirectiveNode renameNode = renameDirective.Directive;
        DirectiveNode sourceDirective = SourceDirective.Create(coordinate);

        return node switch
        {
            DirectiveDefinitionNode directiveDefinitionNode
                => directiveDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            EnumTypeDefinitionNode enumTypeDefinitionNode
                => enumTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode
                => inputObjectTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            InterfaceTypeDefinitionNode interfaceTypeDefinitionNode
                => interfaceTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            ObjectTypeDefinitionNode objectTypeDefinitionNode
                => objectTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            ScalarTypeDefinitionNode scalarTypeDefinitionNode
                => scalarTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            UnionTypeDefinitionNode unionTypeDefinitionNode
                => unionTypeDefinitionNode.WithName(renameDirective.NewName)
                    .ReplaceDirective(renameNode, sourceDirective),

            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    public DocumentNode Rewrite(TContext context)
    {
        return base.Rewrite(context.Document, context) as DocumentNode;
    }
}
