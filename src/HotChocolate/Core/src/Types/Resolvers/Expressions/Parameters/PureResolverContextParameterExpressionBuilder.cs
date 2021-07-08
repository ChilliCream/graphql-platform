using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class PureResolverContextParameterExpressionBuilder
        : IParameterExpressionBuilder
    {
        public ArgumentKind Kind => ArgumentKind.Context;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter)
            => typeof(IPureResolverContext) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Expression context)
            => context;
    }
}
