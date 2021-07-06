using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetContextCompiler<TContext>
        : ResolverParameterCompilerBase
        where TContext : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => parameter.ParameterType.IsAssignableFrom(typeof(TContext));

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => typeof(TContext) == ContextType
                ? context
                : Expression.Convert(context, typeof(TContext));
    }
}
