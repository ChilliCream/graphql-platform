using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal class ScopedContextDataScopedVariableResolver
        : IScopedVariableResolver
    {
        public ScopedVariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            IInputType targetType)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (targetType is null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (!ScopeNames.ScopedContextData.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(
                    ScopedCtxDataScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            context.ScopedContextData.TryGetValue(variable.Name.Value, out object? data);

            IValueNode literal = data switch
            {
                IValueNode l => l,
                null => NullValueNode.Default,
                _ => targetType.ParseValue(data)
            };

            return new ScopedVariableValue
            (
                variable.ToVariableName(),
                targetType.ToTypeNode(),
                literal,
                null
            );
        }
    }
}
