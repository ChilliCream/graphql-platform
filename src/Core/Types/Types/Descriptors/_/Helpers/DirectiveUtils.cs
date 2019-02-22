using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal static class DirectiveUtils
    {
        public static void AddDirective<T>(
            this IHasDirectiveDescriptions directivesContainer,
            T directive)
            where T : class
        {
            if (directive is null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            switch (directive)
            {
                case DirectiveNode node:
                    directivesContainer.Directives.Add(
                        new DirectiveDescription(node));
                    break;
                case string s:
                    AddDirective(
                        directivesContainer,
                        new NameString(s),
                        Array.Empty<ArgumentNode>());
                    break;
                default:
                    directivesContainer.Directives.Add(
                        new DirectiveDescription(directive));
                    break;
            }
        }

        public static void AddDirective(
            this IHasDirectiveDescriptions directivesContainer,
            NameString name,
            IEnumerable<ArgumentNode> arguments)
        {
            directivesContainer.Directives.Add(new DirectiveDescription(
                new DirectiveNode(
                    name.EnsureNotEmpty(nameof(name)),
                    arguments.ToArray())));
        }
    }
}
