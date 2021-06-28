using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal class ContextDataScopedVariableResolver
        : IScopedVariableResolver
    {
        public ScopedVariableValue Resolve(
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

            if (!ScopeNames.ContextData.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(
                    ContextDataScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            context.ContextData.TryGetValue(variable.Name.Value, out object? data);

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
