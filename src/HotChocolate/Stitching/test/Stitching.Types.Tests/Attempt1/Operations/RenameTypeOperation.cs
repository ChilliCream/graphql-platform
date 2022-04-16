using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal sealed class RenameTypeOperation : ISchemaNodeRewriteOperation
{
    public bool CanHandle(ISchemaNode node)
    {
        return node.Definition is DirectiveNode directiveNode
               && node.Parent?.Definition is ITypeDefinitionNode
               && RenameDirective.CanHandle(directiveNode);
    }

    public void Handle(ISchemaNode node)
    {
        ISchemaNode? parent = node.Parent;
        if (parent?.Definition is not IHasName hasName)
        {
            throw new InvalidOperationException("Parent must be a named syntax node");
        }

        ISchemaDatabase database = node.Coordinate.Database;
        var directiveNode = node.Definition as DirectiveNode;
        var renameDirective = new RenameDirective(directiveNode!);
        var sourceDirective = new SourceDirective(parent);

        RenameNode(parent,
            renameDirective,
            sourceDirective,
            database);

        RenameNodeReferences(node,
            database,
            hasName.Name,
            renameDirective.NewName!);
    }

    private static void RenameNode(
        ISchemaNode parent,
        RenameDirective renameDirective,
        SourceDirective sourceDirective,
        ISchemaDatabase nodeDatabase)
    {
        switch (parent.Definition)
        {
            case InterfaceTypeDefinitionNode interfaceTypeDefinitionNode when renameDirective.NewName is not null:
                InterfaceTypeDefinitionNode interfaceReplacement = interfaceTypeDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(interfaceReplacement);
                break;

            case ObjectTypeDefinitionNode objectTypeDefinitionNode when renameDirective?.NewName is not null:
                ObjectTypeDefinitionNode objectTypeReplacement = objectTypeDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(objectTypeReplacement);
                break;

            default:
                throw new NotSupportedException();
        }

        nodeDatabase.Reindex(parent);
    }

    private static void RenameNodeReferences(
        ISchemaNode node,
        ISchemaDatabase schemaDatabase,
        NameNode sourceName,
        NameNode newName)
    {
        DocumentDefinition documentDefinition = node.GetAncestors()
            .OfType<DocumentDefinition>()
            .Last();

        IEnumerable<ISchemaNode> descendentNodes = documentDefinition
            .DescendentNodes(schemaDatabase)
            .ToList();

        var typeReferenceNodes = descendentNodes
            .Where(x => x.Definition is NamedTypeNode typeNode && sourceName.Equals(typeNode.Name))
            .ToList();

        foreach (ISchemaNode? typeReferenceNode in typeReferenceNodes)
        {
            ISchemaNode schemaNode = typeReferenceNode.GetAncestors()
                .First(x => x.Definition is not ITypeNode);

            ISchemaNodeInfo<ITypeNode> rewrittenNode = TypeNodeRewriteHelper.Rewrite(
                typeReferenceNode,
                new NamedTypeNode(newName));

            ISchemaNode? referencedTypeReplacement = default;
            switch (schemaNode)
            {
                case ObjectTypeDefinition objectTypeDefinition:
                    referencedTypeReplacement = objectTypeDefinition
                        .RewriteDefinition(typeReferenceNode, rewrittenNode.Definition);

                    break;

                case FieldDefinition fieldDefinition:
                    referencedTypeReplacement = fieldDefinition
                        .RewriteDefinition(fieldDefinition.Definition.WithType(rewrittenNode.Definition));
                    break;
            }

            if (referencedTypeReplacement is not null)
            {
                schemaDatabase.Reindex(referencedTypeReplacement.Parent ?? referencedTypeReplacement);
            }
        }
    }
}
