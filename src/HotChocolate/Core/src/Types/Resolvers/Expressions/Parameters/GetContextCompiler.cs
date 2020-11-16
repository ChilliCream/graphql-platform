using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetContextCompiler<T, TContext>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
        where TContext : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            parameter.ParameterType.IsAssignableFrom(typeof(TContext));

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            if (typeof(TContext) == ContextType)
            {
                return context;
            }

            return Expression.Convert(context, typeof(TContext));
        }
    }
}
