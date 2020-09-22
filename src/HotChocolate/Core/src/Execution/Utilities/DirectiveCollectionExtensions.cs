using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal static class DirectiveCollectionExtensions
    {
        public static IValueNode? SkipValue(this IReadOnlyList<DirectiveNode> directives)
        {
            DirectiveNode? directive = directives.GetSkipDirective();
            return directive is null ? null : GetIfArgumentValue(directive);
        }

        public static IValueNode? IncludeValue(this IReadOnlyList<DirectiveNode> directives)
        {
            DirectiveNode? directive = directives.GetIncludeDirective();
            return directive is null ? null : GetIfArgumentValue(directive);
        }

        public static bool IsDeferrable(this InlineFragmentNode fragmentNode) =>
            fragmentNode.Directives.GetDeferDirective() is not null;

        public static bool IsDeferrable(this FragmentSpreadNode fragmentSpreadNode) =>
            fragmentSpreadNode.Directives.GetDeferDirective() is not null;

        public static bool IsDeferrable(this IReadOnlyList<DirectiveNode> directives) =>
            directives.GetDeferDirective() is not null;

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

        private static DirectiveNode? GetSkipDirective(
            this IReadOnlyList<DirectiveNode> directives) =>
            GetDirective(directives, WellKnownDirectives.Skip);

        private static DirectiveNode? GetIncludeDirective(
            this IReadOnlyList<DirectiveNode> directives) =>
            GetDirective(directives, WellKnownDirectives.Include);

        private static DirectiveNode? GetDeferDirective(
            this IReadOnlyList<DirectiveNode> directives) =>
            GetDirective(directives, WellKnownDirectives.Defer);

        private static DirectiveNode? GetDirective(
            this IReadOnlyList<DirectiveNode> directives,
            string name)
        {
            for (var i = 0; i < directives.Count; i++)
            {
                DirectiveNode directive = directives[i];
                if (directive.Name.Value.EqualsOrdinal(name))
                {
                    return directive;
                }
            }
            return null;
        }
    }
}
