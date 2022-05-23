using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Directives;

public class SourceDirective
{
    private static readonly NameNode _sourceNameNode = new("source");
    private static readonly NameNode _coordinateNameNode = new("coordinate");

    public SourceDirective(DirectiveNode directiveNode, SchemaCoordinate coordinate)
    {
        Directive = directiveNode;
        Coordinate = coordinate;
    }

    public DirectiveNode Directive { get; }

    public SchemaCoordinate Coordinate { get; }

    public static bool TryParse(ISyntaxNode syntaxNode,
        [NotNullWhen(true)] out SourceDirective? sourceDirective)
    {
        if (syntaxNode is not DirectiveNode directiveNode)
        {
            sourceDirective = default;
            return false;
        }

        return TryParse(directiveNode, out sourceDirective);
    }

    public static bool TryParse(DirectiveNode directiveNode,
        [NotNullWhen(true)] out SourceDirective? sourceDirective)
    {
        if (!SyntaxComparer.BySyntax.Equals(directiveNode.Name, _sourceNameNode))
        {
            sourceDirective = default;
            return false;
        }

        IValueNode? coordinateArgument = directiveNode.Arguments
            .FirstOrDefault(x => x.Name.Equals(_coordinateNameNode))?.Value;

        if (coordinateArgument is not StringValueNode { Value.Length: > 0 } validCoordinateArgument)
        {
            sourceDirective = default;
            return false;
        }

        if (!SchemaCoordinate.TryParse(validCoordinateArgument.Value, out SchemaCoordinate? coordinate))
        {
            sourceDirective = default;
            return false;
        }

        sourceDirective = new SourceDirective(directiveNode, coordinate.Value);
        return true;
    }

    public static DirectiveNode Create(SchemaCoordinate coordinate)
    {
        return new DirectiveNode(_sourceNameNode,
            new List<ArgumentNode> { new(_coordinateNameNode, new StringValueNode(coordinate.ToString())) });
    }
}
