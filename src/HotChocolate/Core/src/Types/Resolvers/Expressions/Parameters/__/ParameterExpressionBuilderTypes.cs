using System;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal static class ParameterExpressionBuilderTypes
    {
        public static Type ContextType { get; } = typeof(IResolverContext);

        public static Type PureContextType { get; } = typeof(IPureResolverContext);
    }
}
