using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    internal static class DirectiveCollectionExtensions
    {
        public static IValueNode SkipValue(
            this IEnumerable<DirectiveNode> directives)
        {
            DirectiveNode directive = directives.GetSkipDirective();
            if (directive == null)
            {
                return null;
            }

            ArgumentNode argumentNode = directive.Arguments.SingleOrDefault();
            if (argumentNode == null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            CoreResources
                                .DirectiveCollectionExtensions_NotValid,
                            directive.Name.Value))
                        .Build());
            }

            return argumentNode.Value;
        }

        public static IValueNode IncludeValue(
            this IEnumerable<DirectiveNode> directives)
        {
            DirectiveNode directive = directives.GetIncludeDirective();
            if (directive == null)
            {
                return null;
            }

            ArgumentNode argumentNode = directive.Arguments.SingleOrDefault();
            if (argumentNode == null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            CoreResources
                                .DirectiveCollectionExtensions_NotValid,
                            directive.Name.Value))
                        .Build());
            }

            return argumentNode.Value;
        }

        public static DirectiveNode GetIncludeDirective(
            this IEnumerable<DirectiveNode> directives)
        {
            return GetDirective(directives, WellKnownDirectives.Include);
        }

        public static DirectiveNode GetSkipDirective(
            this IEnumerable<DirectiveNode> directives)
        {
            return GetDirective(directives, WellKnownDirectives.Skip);
        }

        private static DirectiveNode GetDirective(
            this IEnumerable<DirectiveNode> directives, string name)
        {
            return directives.FirstOrDefault(t => t.Name.Value == name);
        }
    }
}
