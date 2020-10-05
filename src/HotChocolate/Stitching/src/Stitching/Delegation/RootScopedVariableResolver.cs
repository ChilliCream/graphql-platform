using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal class RootScopedVariableResolver
        : IScopedVariableResolver
    {
        private readonly Dictionary<string, IScopedVariableResolver> _resolvers =
            new Dictionary<string, IScopedVariableResolver>
            {
                { ScopeNames.Arguments, new ArgumentScopedVariableResolver() },
                { ScopeNames.Fields, new FieldScopedVariableResolver() },
                { ScopeNames.ContextData, new ContextDataScopedVariableResolver() },
                { ScopeNames.ScopedContextData, new ScopedContextDataScopedVariableResolver() }
            };

        public VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            IInputType targetType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (_resolvers.TryGetValue(variable.Scope.Value, out IScopedVariableResolver? resolver))
            {
                return resolver.Resolve(context, variable, targetType);
            }

            throw new QueryException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.RootScopedVariableResolver_ScopeNotSupported,
                    variable.Scope.Value)
                .SetCode(ErrorCodes.Stitching.ScopeNotDefined)
                .SetPath(context.Path)
                .AddLocation(context.FieldSelection)
                .Build());
        }
    }
}
