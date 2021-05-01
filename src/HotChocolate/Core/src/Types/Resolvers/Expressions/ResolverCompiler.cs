using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers.Expressions.Parameters;

#nullable enable

namespace HotChocolate.Resolvers.Expressions
{
    internal class ResolverCompiler
    {
        private readonly IResolverParameterCompiler[] _compilers;

        protected ResolverCompiler()
            : this(ParameterCompilerFactory.Create())
        {
        }

        protected ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
        {
            if (compilers is null)
            {
                throw new ArgumentNullException(nameof(compilers));
            }

            _compilers = compilers.ToArray();
            Context = Expression.Parameter(typeof(IResolverContext), "context");
        }

        protected ParameterExpression Context { get; }

        protected static MethodInfo Parent { get; } =
            typeof(IResolverContext).GetMethod(nameof(IResolverContext.Parent))!;

        protected static MethodInfo Resolver { get; } =
            typeof(IResolverContext).GetMethod(nameof(IResolverContext.Resolver))!;

        protected IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IResolverParameterCompiler? parameterCompiler =
                    _compilers.FirstOrDefault(t =>
                        t.CanHandle(parameter, sourceType));

                if (parameterCompiler is null)
                {
                    throw new InvalidOperationException(
                        TypeResources.ResolverCompiler_UnknownParameterType);
                }

                yield return parameterCompiler.Compile(
                    Context, parameter, sourceType);
            }
        }

        public static SubscribeCompiler Subscribe { get; } = new();

        public static ResolveCompiler Resolve { get; } = new();
    }
}
