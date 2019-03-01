using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Delegation
{
    internal class ContextDataScopedVariableResolver
        : IScopedVariableResolver
    {
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

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (!ScopeNames.Arguments.Equals(variable.Scope.Value))
            {
                // TODO : RESOURCES
                throw new ArgumentException("NOT SUPPORTED",
                    nameof(variable));
            }

            context.ContextData.TryGetValue(variable.Name.Value,
                out object data);

            return new VariableValue
            (
                variable.ToVariableName(),
                targetType,
                data,
                null
            );
        }
    }
}
