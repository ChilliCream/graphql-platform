using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class ResolverParameterCompilerBase : IResolverParameterCompiler
    {
        protected ResolverParameterCompilerBase()
        {
            ContextType = typeof(IResolverContext);
            PureContextType = typeof(IPureResolverContext);
        }

        protected Type ContextType { get; }

        protected Type PureContextType { get; }

        public abstract bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        public abstract Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType);
    }
}
