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
        }

        protected Type ContextType { get; }

        protected TypeInfo ContextTypeInfo { get; }

        public abstract bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        public abstract Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType);
    }
}
