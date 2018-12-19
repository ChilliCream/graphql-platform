using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal static class SyntaxNodeExtensions
    {
        private const string _deprecated = "deprecated";
        private const string _deprecationReason = "reason";

        public static string DeprecationReason(this Language.IHasDirectives syntaxNode)
        {
            DirectiveNode directive = syntaxNode.Directives
                .FirstOrDefault(t => t.Name.Value == _deprecated);
            if (directive == null)
            {
                return null;
            }

            ArgumentNode argument = directive.Arguments
                .FirstOrDefault(t => t.Name.Value == _deprecationReason);
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
