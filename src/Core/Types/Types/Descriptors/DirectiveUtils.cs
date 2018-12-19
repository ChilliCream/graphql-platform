using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal static class DirectiveUtils
    {
        public static void AddDirective<T>(
            this IList<DirectiveDescription> directives,
            T directive)
            where T : class
        {
            if (directive is null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            if (directive is DirectiveNode node)
            {
                directives.Add(new DirectiveDescription(node));
            }
            else
            {
                directives.Add(new DirectiveDescription(directive));
            }
        }

        public static void AddDirective(
            this IList<DirectiveDescription> directives,
            NameString name,
            IEnumerable<ArgumentNode> arguments)
        {
            directives.Add(new DirectiveDescription(
                new DirectiveNode(
                    name.EnsureNotEmpty(nameof(name)),
                    arguments.ToArray())));
        }
    }
}
