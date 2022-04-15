using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal sealed class RenameOperation : ISchemaNodeRewriteOperation
{
    public bool CanHandle(ISchemaNode node)
    {
        return node.Definition is DirectiveNode directiveNode
               && RenameDirective.CanHandle(directiveNode);
    }

    public void Handle(ISchemaNode node)
    {
        ISchemaCoordinate2Provider coordinateProvider = node.Coordinate.Provider;
        var directiveNode = node.Definition as DirectiveNode;
        var renameDirective = new RenameDirective(directiveNode!);

        ISchemaNode? parent = node.Parent;
        var sourceDirective = new SourceDirective(node.Parent.Coordinate);

        ISyntaxNode replacement;
        switch (parent.Definition)
        {
            case InterfaceTypeDefinitionNode interfaceTypeDefinitionNode when renameDirective?.NewName is not null:
                replacement = interfaceTypeDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(replacement);
                break;

            case ObjectTypeDefinitionNode objectTypeDefinitionNode when renameDirective?.NewName is not null:
                replacement = objectTypeDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(replacement);
                break;

            case FieldDefinitionNode fieldDefinitionNode when renameDirective?.NewName is not null:
                replacement = fieldDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);

                parent.RewriteDefinition(replacement);
                break;
        }

        DocumentDefinition documentDefinition = node.GetAncestors()
            .OfType<DocumentDefinition>()
            .Last();

        var typeReferenceNodes = documentDefinition
            .DescendentNodes(coordinateProvider)
            .Where(x => x.Definition is ITypeNode typeNode && typeNode.IsEqualTo(new NamedTypeNode("Test")))
            .ToList();

        foreach (ISchemaNode? typeReferenceNode in typeReferenceNodes)
        {
            ISchemaNode schemaNode = typeReferenceNode.GetAncestors()
                .OfType<FieldDefinition>()
                .First();

            ISchemaNodeInfo<ITypeNode> rewrittenNode = TypeNodeRewriteHelper.Rewrite(
                typeReferenceNode,
                new NamedTypeNode("Test_Renamed"));

            switch (schemaNode)
            {
                case FieldDefinition fieldDefinition:
                    FieldDefinitionNode fieldNode = fieldDefinition.Definition;
                    fieldDefinition.RewriteDefinition(new FieldDefinitionNode(default, fieldNode.Name,
                        fieldNode.Description,
                        fieldNode.Arguments,
                        rewrittenNode.Definition,
                        fieldNode.Directives));

                    break;

                default:
                    continue;
            }
        }
    }
}

internal class SourceDirective
{
    public ISchemaCoordinate2 Coordinate { get; }

    public SourceDirective(ISchemaCoordinate2 coordinate)
    {
        Coordinate = coordinate;
        Node = new DirectiveNode("_hc_source",
            new ArgumentNode("coordinate", SchemaCoordinatePrinter.Print(coordinate)));
    }

    public DirectiveNode Node { get; }
}

internal sealed class RenameDirective
{
    public DirectiveNode Node { get; }
    public NameNode? NewName
    {
        get
        {
            var nameArgument = Node.Arguments.FirstOrDefault(x => x.Name.Value.Equals("name"))?.Value.Value;
            if (nameArgument is not string stringArgument || string.IsNullOrEmpty(stringArgument))
            {
                return default;
            }
            return new NameNode(stringArgument);
        }
    }

    public RenameDirective(DirectiveNode node)
    {
        Node = node;
    }

    public static bool CanHandle(DirectiveNode directiveNode)
    {
        return directiveNode.Name.Equals(new NameNode("rename"));
    }
}
