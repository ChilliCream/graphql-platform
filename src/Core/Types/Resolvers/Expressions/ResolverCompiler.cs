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
        private static readonly MethodInfo _parent =
            typeof(IResolverContext).GetMethod("Parent")!;
        private static readonly MethodInfo _resolver =
            typeof(IResolverContext).GetMethod("Resolver")!;

        private readonly IResolverParameterCompiler[] _compilers;
        private readonly ParameterExpression _context;

        protected ResolverCompiler()
            : this(ParameterCompilerFactory.Create())
        {
        }

        protected ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
        {
            _compilers = compilers?.ToArray() ??
                throw new ArgumentNullException(nameof(compilers));
            _context = Expression.Parameter(typeof(IResolverContext), "context");
        }

        protected ParameterExpression Context => _context;

        protected static MethodInfo Parent => _parent;

        protected static MethodInfo Resolver => _resolver;

        protected IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IResolverParameterCompiler parameterCompiler =
                    _compilers.FirstOrDefault(t =>
                        t.CanHandle(parameter, sourceType));

                if (parameterCompiler == null)
                {
                    throw new InvalidOperationException(
                        TypeResources.ResolverCompiler_UnknownParameterType);
                }

                yield return parameterCompiler.Compile(
                    _context, parameter, sourceType);
            }
        }

        public static SubscribeCompiler Subscribe { get; } = new SubscribeCompiler();

        public static ResolveCompiler Resolve { get; } = new ResolveCompiler();
    }
}
