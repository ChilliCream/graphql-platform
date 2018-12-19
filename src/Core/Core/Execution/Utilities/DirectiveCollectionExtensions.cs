using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal static class DirectiveCollectionExtensions
    {
        public static bool Include(
            this IEnumerable<DirectiveNode> directives,
            VariableCollection variables)
        {
            return directives.GetIncludeDirective()
                .EvaluateDirective(variables) ?? true;
        }

        public static bool Skip(
            this IEnumerable<DirectiveNode> directives,
            VariableCollection variables)
        {
            return directives.GetSkipDirective()
                .EvaluateDirective(variables) ?? false;
        }

        private static bool? EvaluateDirective(
            this DirectiveNode directive,
            VariableCollection variables)
        {
            if (directive == null)
            {
                return null;
            }

            ArgumentNode argumentNode = directive.Arguments.SingleOrDefault();
            if (argumentNode == null)
            {
                throw new QueryException(new QueryError(
                    $"The {directive.Name.Value} attribute is not valid."));
            }

            if (argumentNode.Value is BooleanValueNode b)
            {
                return b.Value;
            }

            if (argumentNode.Value is VariableNode v)
            {
                return variables.GetVariable<bool>(v.Name.Value);
            }

            throw new QueryException(new QueryError(
                $"The {directive.Name.Value} if-argument value has to be a 'Boolean'."));
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
