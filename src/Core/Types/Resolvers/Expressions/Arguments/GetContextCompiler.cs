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
            typeof(TContext) == parameter.ParameterType;

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            if (typeof(TContext) == ContextType)
            {
                return Context;
            }

            return Expression.Convert(Context, typeof(TContext));
        }
    }
}
