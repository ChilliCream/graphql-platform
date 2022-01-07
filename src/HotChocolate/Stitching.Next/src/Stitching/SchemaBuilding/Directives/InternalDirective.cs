using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct InternalDirective : ISchemaBuildingDirective
{
    public DirectiveKind Kind => DirectiveKind.Internal;

    public static bool TryParse(DirectiveNode directiveSyntax, out InternalDirective directive)
    {
        if (directiveSyntax is null)
        {
            throw new ArgumentNullException(nameof(directiveSyntax));
        }

        if (directiveSyntax.Name.Value.EqualsOrdinal("internal") &&
            directiveSyntax.Arguments.Count is 0)
        {
            directive = new();
            return true;
        }

        directive = default;
        return false;
    }

    public static bool TryParseFirst(IHasDirectives syntaxNode, out InternalDirective directive)
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

    public static bool HasOne(IHasDirectives syntaxNode)
    {
        foreach (DirectiveNode directiveSyntax in syntaxNode.Directives)
        {
            if (IsOf(directiveSyntax))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsOf(DirectiveNode directiveSyntax)
    {
        if (directiveSyntax is null)
        {
            throw new ArgumentNullException(nameof(directiveSyntax));
        }

        if (directiveSyntax.Name.Value.EqualsOrdinal("internal") &&
            directiveSyntax.Arguments.Count is 0)
        {
            return true;
        }
        
        return false;
    }
}
