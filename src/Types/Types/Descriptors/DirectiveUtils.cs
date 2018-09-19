using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
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
            string name,
            IEnumerable<ArgumentNode> arguments)
        {
            name.EnsureDirectiveNameIsValid();
            directives.Add(new DirectiveDescription(
                new DirectiveNode(name, arguments.ToArray())));
        }

        public static void EnsureDirectiveNameIsValid(this string directiveName)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentException(
                    "The directive name cannot be null or empty.",
                    nameof(directiveName));
            }

            if (!ValidationHelper.IsTypeNameValid(directiveName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL directive name.",
                    nameof(directiveName));
            }
        }
    }
}
