using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal static class SyntaxNodeExtensions
    {
        public static string DeprecationReason(
            this Language.IHasDirectives syntaxNode)
        {
            DirectiveNode directive = syntaxNode.Directives.FirstOrDefault(t =>
                t.Name.Value == WellKnownDirectives.Deprecated);

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
        {
            if (directiveNode is null)
            {
                throw new System.ArgumentNullException(nameof(directiveNode));
            }

            return string.Equals(directiveNode.Name.Value,
                WellKnownDirectives.Deprecated,
                StringComparison.Ordinal);
        }
    }
}
