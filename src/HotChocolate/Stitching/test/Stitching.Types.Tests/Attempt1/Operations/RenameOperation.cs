using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal sealed class RenameOperation : ISchemaNodeRewriteOperation<INamedSyntaxNode>
{
    public INamedSyntaxNode Apply(INamedSyntaxNode source, ISchemaCoordinate2 coordinate, OperationContext context)
    {
        RenameDirective? renameDirective = source.Directives
            .Where(RenameDirective.CanHandle)
            .Select(directive => new RenameDirective(directive))
            .FirstOrDefault();

        var sourceDirective = new SourceDirective(coordinate);

        switch (source)
        {
            case ObjectTypeDefinitionNode objectTypeDefinitionNode when renameDirective?.NewName is not null:
                return objectTypeDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);
            case FieldDefinitionNode fieldDefinitionNode when renameDirective?.NewName is not null:
                return fieldDefinitionNode
                    .WithName(renameDirective.NewName)
                    .ModifyDirectives(add: sourceDirective.Node, remove: renameDirective.Node);
        }

        throw new NotSupportedException();
    }

    public ISchemaCoordinate2 Match { get; } = new SchemaCoordinate2(SyntaxKind.Directive, new NameNode("rename"));
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
