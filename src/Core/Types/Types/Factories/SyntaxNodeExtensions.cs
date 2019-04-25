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
            if (directive == null)
            {
                return null;
            }

            ArgumentNode argument = directive.Arguments.FirstOrDefault(t =>
                t.Name.Value == WellKnownDirectives.DeprecationReasonArgument);
            if (argument == null)
            {
                return null;
            }

            if (argument.Value is StringValueNode s)
            {
                return s.Value;
            }

            return null;
        }

        public static bool IsDeprecationReason(this DirectiveNode directiveNode)
        {
            if (directiveNode == null)
            {
                throw new System.ArgumentNullException(nameof(directiveNode));
            }

            return string.Equals(directiveNode.Name.Value,
                WellKnownDirectives.Deprecated,
                StringComparison.Ordinal);
        }
    }
}
