using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct IsDirective : ISchemaBuildingDirective
{
    public IsDirective(SchemaCoordinate coordinate)
    {
        Coordinate = coordinate;
    }

    public DirectiveKind Kind => DirectiveKind.Is;

    public SchemaCoordinate Coordinate { get; }

    public static bool TryParse(DirectiveNode directiveSyntax, out IsDirective directive)
    {
        if (directiveSyntax is null)
        {
            throw new ArgumentNullException(nameof(directiveSyntax));
        }

        if (directiveSyntax.Name.Value.EqualsOrdinal("is") &&
            directiveSyntax.Arguments.Count is 1)
        {
            ArgumentNode argument = directiveSyntax.Arguments[0];
            if (argument.Name.Value.EqualsOrdinal("a") &&
                argument.Value.Kind is SyntaxKind.StringValue &&
                SchemaCoordinate.TryParse((string)argument.Value.Value!, out var coordinate))
            {
                directive = new IsDirective(coordinate.Value);
                return true;
            }
        }

        directive = default;
        return false;
    }

    public static bool TryParseFirst(IHasDirectives syntaxNode, out IsDirective directive)
    {
        foreach (DirectiveNode directiveSyntax in syntaxNode.Directives)
        {
            if (IsDirective.TryParse(directiveSyntax, out directive))
            {
                return true;
            }
        }

        directive = default;
        return false;
    }
}
