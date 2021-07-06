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
    internal abstract class ResolverCompiler
    {
        private readonly IResolverParameterCompiler[] _compilers;

        protected ResolverCompiler() : this(ParameterCompilerFactory.Create())
        {
        }

        private ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
        {
            if (compilers is null)
            {
                throw new ArgumentNullException(nameof(compilers));
            }

            _compilers = compilers.ToArray();
            Context = Expression.Parameter(typeof(IResolverContext), "context");
            PureContext = Expression.Parameter(typeof(IPureResolverContext), "context");
        }

        protected ParameterExpression Context { get; }

        protected ParameterExpression PureContext { get; }

        protected static MethodInfo Parent { get; } =
            typeof(IPureResolverContext).GetMethod(nameof(IPureResolverContext.Parent))!;

        protected static MethodInfo Resolver { get; } =
            typeof(IPureResolverContext).GetMethod(nameof(IPureResolverContext.Resolver))!;

        protected Expression[] CreateParameters(
            ParameterExpression context, 
            ParameterInfo[] parameters, 
            Type sourceType)
        {
            var parameterResolvers = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                IResolverParameterCompiler? parameterCompiler =
                    _compilers.FirstOrDefault(t => t.CanHandle(parameter, sourceType));

                if (parameterCompiler is null)
                {
                    throw new InvalidOperationException(
                        TypeResources.ResolverCompiler_UnknownParameterType);
                }

                parameterResolvers[i] = parameterCompiler.Compile(context, parameter, sourceType);
            }

            return parameterResolvers;
        }

        public static SubscribeCompiler Subscribe { get; } = new();

        public static ResolveCompiler Resolve { get; } = new();
    }
}
