using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class ResolverContextParameterExpressionBuilder : IParameterExpressionBuilder
    {
        public ArgumentKind Kind => ArgumentKind.Context;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter)
            => typeof(IResolverContext) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Expression context)
            => context;
    }
}
