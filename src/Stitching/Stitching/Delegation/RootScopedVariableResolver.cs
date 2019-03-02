using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Delegation
{
    internal class RootScopedVariableResolver
        : IScopedVariableResolver
    {
        public RootScopedVariableResolver()
        {
            Resolvers[ScopeNames.Arguments] =
                new ArgumentScopedVariableResolver();
            Resolvers[ScopeNames.Fields] =
                new FieldScopedVariableResolver();
            Resolvers[ScopeNames.ContextData] =
                new ContextDataScopedVariableResolver();
            Resolvers[ScopeNames.ScopedContextData] =
                new ScopedContextDataScopedVariableResolver();
        }

        private Dictionary<string, IScopedVariableResolver> Resolvers { get; } =
            new Dictionary<string, IScopedVariableResolver>();

        public VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            ITypeNode targetType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (Resolvers.TryGetValue(variable.Scope.Value,
                out IScopedVariableResolver resolver))
            {
                return resolver.Resolve(context, variable, targetType);
            }

            throw new QueryException(QueryError.CreateFieldError(
                string.Format(CultureInfo.InvariantCulture,
                    StitchingResources
                        .RootScopedVariableResolver_ScopeNotSupported,
                    variable.Scope.Value),
                context.Path,
                context.FieldSelection)
                .WithCode(ErrorCodes.ScopeNotDefined));
        }
    }
}
