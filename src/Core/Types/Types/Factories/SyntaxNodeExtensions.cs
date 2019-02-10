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
    }
}
