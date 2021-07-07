using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class ResolverContextParameterExpressionBuilder : IParameterExpressionBuilder
    {
        public ArgumentKind Kind => ArgumentKind.Context;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(IResolverContext) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => context;
    }
}
