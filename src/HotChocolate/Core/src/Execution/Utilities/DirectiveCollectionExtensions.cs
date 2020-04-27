using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    internal static class DirectiveCollectionExtensions
    {
        public static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
        {

            DirectiveNode directive = directives.GetSkipDirective();

            if (directive == null)
            {
                return null;
            }

            return GetIfArgumentValue(directive);
        }

        public static IValueNode IncludeValue(
            this IReadOnlyList<DirectiveNode> directives)
        {
            DirectiveNode directive = directives.GetIncludeDirective();

            if (directive == null)
            {
                return null;
            }

            return GetIfArgumentValue(directive);
        }

        private static IValueNode GetIfArgumentValue(DirectiveNode directive)
        {
            if (directive.Arguments.Count == 1)
            {
                ArgumentNode argument = directive.Arguments[0];
                if (string.Equals(
                    argument.Name.Value,
                    WellKnownDirectives.IfArgument,
                    StringComparison.Ordinal))
                {
                    return argument.Value;
                }
            }

            throw ThrowHelper.MissingIfArgument(directive);
        }

        private static DirectiveNode GetIncludeDirective(
            this IReadOnlyList<DirectiveNode> directives) =>
            GetDirective(directives, WellKnownDirectives.Include);

        private static DirectiveNode GetSkipDirective(
            this IReadOnlyList<DirectiveNode> directives) =>
            GetDirective(directives, WellKnownDirectives.Skip);

        private static DirectiveNode? GetDirective(
            this IReadOnlyList<DirectiveNode> directives,
            string name)
        {
            for (var i = 0; i < directives.Count; i++)
            {
                if (string.Equals(directives[i].Name.Value, name, StringComparison.Ordinal))
                {
                    return directives[i];
                }
            }

            return null;
        }
    }
}
