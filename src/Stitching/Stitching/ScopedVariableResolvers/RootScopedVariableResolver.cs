using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    internal class RootScopedVariableResolver
        : IScopedVariableResolver
    {
        private const string _argumentScope = "arguments";
        private const string _fieldScope = "fields";
        private const string _variableScope = "variables";

        public RootScopedVariableResolver()
        {
            Resolvers[_argumentScope] = new ArgumentScopedVariableResolver();
            Resolvers[_fieldScope] = new FieldScopedVariableResolver();
            Resolvers[_variableScope] = new VariableScopedVariableResolver();
        }

        private Dictionary<string, IScopedVariableResolver> Resolvers { get; } =
            new Dictionary<string, IScopedVariableResolver>();

        public VariableValue Resolve(
            IMiddlewareContext context,
            IReadOnlyDictionary<string, object> variables,
            ScopedVariableNode variable)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (Resolvers.TryGetValue(variable.Scope.Value,
                out IScopedVariableResolver resolver))
            {
                return resolver.Resolve(context, variables, variable);
            }

             throw new QueryException(QueryError.CreateFieldError(
                $"The specified scope `{variable.Scope.Value}` " +
                "is not supported.",
                context.Path,
                context.FieldSelection)
                .WithCode(ErrorCodes.ScopeNotDefined));
        }
    }
}
