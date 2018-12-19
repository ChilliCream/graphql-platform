
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal static class StitchingAstExtensions
    {
        private static readonly HashSet<string> _stitchingDirectives =
            new HashSet<string>
            {
                    DirectiveNames.Schema,
                    DirectiveNames.Delegate
            };

        public static string GetSchemaName(this FieldNode field)
        {
            DirectiveNode directive = field.Directives
                .SingleOrDefault(t => IsSchemaDirective(t));

            if (directive == null)
            {
                throw new ArgumentException(
                    "The specified field is not annotated.");
            }

            ArgumentNode argument = directive.Arguments
                .SingleOrDefault(t => t.IsNameArgument());

            if (argument == null)
            {
                throw new ArgumentException(
                    "The schema directive has to have a name argument.");
            }

            if (argument.Value is StringValueNode value)
            {
                return value.Value;
            }

            throw new ArgumentException(
                "The schema directive name attribute " +
                "has to be a string value.");
        }

        public static bool IsSchemaDirective(
            this DirectiveNode directive)
        {
            return directive.Name.Value.EqualsOrdinal(DirectiveNames.Schema);
        }

        public static bool IsDelegateDirective(
            this DirectiveNode directive)
        {
            return directive.Name.Value.EqualsOrdinal(DirectiveNames.Delegate);
        }

        public static bool IsSchemaDirective(
            this DirectiveNode directive,
            string schemaName)
        {
            if (IsSchemaDirective(directive))
            {
                ArgumentNode argument = directive.Arguments
                    .SingleOrDefault(t => t.IsNameArgument());
                if (argument.Value is StringValueNode value
                    && value.Value.EqualsOrdinal(schemaName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsStitchingDirective(this DirectiveNode directive)
        {
            return _stitchingDirectives.Contains(directive.Name.Value);
        }

        private static bool IsNameArgument(this ArgumentNode argument)
        {
            return argument.Name.Value.EqualsOrdinal("name");
        }
    }
}
