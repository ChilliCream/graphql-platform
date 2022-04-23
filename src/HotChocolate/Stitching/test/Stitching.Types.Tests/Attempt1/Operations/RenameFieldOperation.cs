using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal sealed class RenameFieldOperation : ISchemaNodeRewriteOperation
{
    public bool CanHandle(ISchemaNode node, RewriteOperationContext context)
    {
        return node.Definition is DirectiveNode directiveNode
               && node.Parent?.Definition is IHasName
               && node.Parent.Definition.Kind == SyntaxKind.FieldDefinition
               && RenameDirective.CanHandle(directiveNode);
    }

    public void Handle(ISchemaNode node, RewriteOperationContext context)
    {
        ISchemaNode? parent = node.Parent;
        if (parent?.Definition is not IHasName hasName)
        {
            throw new InvalidOperationException("Parent must be a named syntax node");
        }

        ISchemaDatabase database = context.Database;
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
            renameDirective);
    }

    private static void RenameNode(
        ISchemaNode parent,
        RenameDirective renameDirective,
        SourceDirective sourceDirective,
        ISchemaDatabase nodeDatabase)
    {
        switch (parent.Definition)
        {
            case FieldDefinitionNode fieldDefinitionNode when renameDirective?.NewName is not null:
                FieldDefinitionNode replacement = fieldDefinitionNode.WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(replacement);
                break;

            default:
                throw new NotSupportedException();
        }

        nodeDatabase.Reindex(parent.Parent ?? parent);
    }

    private static void RenameNodeReferences(ISchemaNode node,
        ISchemaDatabase schemaDatabase,
        NameNode sourceName,
        RenameDirective renameDirective)
    {
        if (node.Parent?.Parent is not InterfaceTypeDefinition interfaceTypeDefinition)
        {
            return;
        }

        DocumentDefinition documentDefinition = node.GetAncestors()
            .OfType<DocumentDefinition>()
            .Last();

        IEnumerable<ISchemaNode> descendentNodes = documentDefinition
            .DescendentNodes(schemaDatabase);

        var objectTypeDefinitions = descendentNodes
            .OfType<ObjectTypeDefinition>()
            .ToList();

        var fields = new List<FieldDefinition>();
        foreach(ObjectTypeDefinition objectTypeDefinition in objectTypeDefinitions)
        {
            IReadOnlyList<NamedTypeNode> interfaces = objectTypeDefinition.Definition.Interfaces;
            var implementsInterface = interfaces.Any(x => x.Name.IsEqualTo(interfaceTypeDefinition.Name));

            if (!implementsInterface)
            {
                continue;
            }

            IEnumerable<FieldDefinition> schemaNodes = objectTypeDefinition
                .DescendentNodes(schemaDatabase)
                .OfType<FieldDefinition>();

            foreach (FieldDefinition field in schemaNodes)
            {
                if (field.Name.IsEqualTo(sourceName))
                {
                    fields.Add(field);
                }
            }
        }

        foreach (FieldDefinition fieldDefinition in fields)
        {
            var fieldSourceDirective = new SourceDirective(fieldDefinition);

            ISchemaNode referencedTypeReplacement = fieldDefinition
                .RewriteDefinition(fieldDefinition.Definition
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: fieldSourceDirective.Node));

            schemaDatabase.Reindex(referencedTypeReplacement.Parent ?? referencedTypeReplacement);
        }
    }
}
