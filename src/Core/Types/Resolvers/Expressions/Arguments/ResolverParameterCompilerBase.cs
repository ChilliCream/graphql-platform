using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class ResolverParameterCompilerBase<T>
        : IResolverParameterCompiler
        where T : IResolverContext
    {
        protected ResolverParameterCompilerBase()
        {
            ContextType = typeof(T);
            ContextTypeInfo = ContextType.GetTypeInfo();
            Context = Expression.Parameter(ContextType);
        }

        protected Type ContextType { get; }

        protected TypeInfo ContextTypeInfo { get; }

        protected Expression Context { get; }

        public abstract bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        public abstract Expression Compile(
            ParameterInfo parameter,
            Type sourceType);
    }
}
