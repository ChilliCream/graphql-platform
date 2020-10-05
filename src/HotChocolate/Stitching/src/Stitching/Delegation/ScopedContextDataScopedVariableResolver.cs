using System;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal class ScopedContextDataScopedVariableResolver
        : IScopedVariableResolver
    {
        private readonly DictionaryToObjectValueConverter _converter =
            new DictionaryToObjectValueConverter();

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

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (!ScopeNames.ScopedContextData.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(StitchingResources
                    .ScopedCtxDataScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            context.ScopedContextData.TryGetValue(variable.Name.Value, out object? data);

            return new VariableValue
            (
                variable.ToVariableName(),
                targetType.ToTypeNode(),
                _converter.Convert(data, targetType, variable.Value),
                null
            );
        }
    }
}
