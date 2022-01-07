using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct SchemaDirective : ISchemaBuildingDirective
{
    public SchemaDirective(NameString name)
    {
        Name = name;
    }

    public DirectiveKind Kind => DirectiveKind.Schema;

    public NameString Name { get; }

    public static bool TryParse(DirectiveNode directiveSyntax, out SchemaDirective directive)
    {
        if (directiveSyntax is null)
        {
            throw new ArgumentNullException(nameof(directiveSyntax));
        }

        if (directiveSyntax.Name.Value.EqualsOrdinal("schema") &&
            directiveSyntax.Arguments.Count is 1)
        {
            ArgumentNode argument = directiveSyntax.Arguments[0];
            if (argument.Name.Value.EqualsOrdinal("name") &&
                argument.Value.Kind is SyntaxKind.StringValue)
            {
                directive = new SchemaDirective((string)argument.Value.Value!);
                return true;
            }
        }

        directive = default;
        return false;
    }

    public static bool TryParseFirst(IHasDirectives syntaxNode, out SchemaDirective directive)
    {
        foreach (DirectiveNode directiveSyntax in syntaxNode.Directives)
        {
            if (TryParse(directiveSyntax, out directive))
            {
                return true;
            }
        }

        directive = default;
        return false;
    }
}
