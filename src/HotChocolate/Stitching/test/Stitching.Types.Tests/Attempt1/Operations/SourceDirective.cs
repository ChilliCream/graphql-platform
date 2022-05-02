using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class SourceDirective
{
    public SourceDirective(ISchemaNode schemaNode)
    {
        var coordinate = CalculateOriginalSources(schemaNode);

        Node = new DirectiveNode("_hc_source",
            new ArgumentNode("coordinate", coordinate));
    }

    public DirectiveNode Node { get; }

    private static string CalculateOriginalSources(ISchemaNode? schemaNode)
    {
        var coordinates = new Stack<ISchemaCoordinate2>();

        while (true)
        {
            if (schemaNode?.Coordinate is null)
            {
                break;
            }

            ISchemaCoordinate2? coordinate = schemaNode.Coordinate;
            if (schemaNode.Definition is IHasDirectives hasDirectives)
            {
                DirectiveNode? sourceDirective = hasDirectives.Directives
                    .LastOrDefault(CanHandle);

                if (sourceDirective is not null)
                {
                    ArgumentNode source = sourceDirective.Arguments
                        .First(x => x.Name.IsEqualTo(new NameNode("coordinate")));

                    var sourceValue = $"{source.Value.Value}";
                    if (string.IsNullOrEmpty(sourceValue))
                    {
                        break;
                    }

                    coordinate = new SchemaCoordinate2(SyntaxKind.Name, new NameNode(sourceValue));
                    coordinates.Push(coordinate);
                    break;
                }
            }

            coordinates.Push(coordinate);

            schemaNode = schemaNode.Parent;
        }

        return SchemaCoordinatePrinter.Print(coordinates.ToList());
    }

    public static bool CanHandle(DirectiveNode directiveNode)
    {
        return directiveNode.Name.Equals(new NameNode("_hc_source"));
    }
}
