using System;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class SdlToTypeSystemHelper
{
    public static void AddDirectives(
        IHasDirectiveDefinition owner,
        HotChocolate.Language.IHasDirectives ownerSyntax)
    {
        foreach (DirectiveNode directive in ownerSyntax.Directives)
        {
            if (!directive.IsDeprecationReason() &&
                !directive.IsBindingDirective())
            {
                owner.Directives.Add(new(directive));
            }
        }
    }

    public static string? DeprecationReason(
        this Language.IHasDirectives syntaxNode)
    {
        DirectiveNode? directive = syntaxNode.Directives.FirstOrDefault(
            t => t.Name.Value == WellKnownDirectives.Deprecated);

        if (directive is null)
        {
            return null;
        }

        if (directive.Arguments.Count != 0
            && directive.Arguments[0].Name.Value ==
                WellKnownDirectives.DeprecationReasonArgument
            && directive.Arguments[0].Value is StringValueNode s
            && !string.IsNullOrEmpty(s.Value))
        {
            return s.Value;
        }

        return WellKnownDirectives.DeprecationDefaultReason;
    }

    public static bool IsDeprecationReason(this DirectiveNode directiveNode)
        => string.Equals(directiveNode.Name.Value,
            WellKnownDirectives.Deprecated,
            StringComparison.Ordinal);
}
