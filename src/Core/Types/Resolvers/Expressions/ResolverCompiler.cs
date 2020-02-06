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
        private readonly IResolverMetadataAnnotator[] _annotators;
        private readonly ParameterExpression _context;

        protected ResolverCompiler()
            : this(ParameterCompilerFactory.Create(),
                  MetadataAnnotationFactory.Create())
        {
        }

        protected ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers,
            IEnumerable<IResolverMetadataAnnotator> annotators)
        {
            _compilers = compilers?.ToArray() ??
                throw new ArgumentNullException(nameof(compilers));
            _annotators = annotators?.ToArray() ??
                throw new ArgumentNullException(nameof(annotators));
            _context = Expression.Parameter(typeof(IResolverContext));
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

        protected ResolverMetadata CreateMetadata(
            ResolverMetadata metadata,
            IEnumerable<ParameterInfo> parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IResolverMetadataAnnotator metadataAnnotator =
                    _annotators.FirstOrDefault(t =>
                        t.CanHandle(parameter, sourceType));

                if (metadataAnnotator != null)
                {
                    metadata = metadataAnnotator.Annotate(
                        metadata, parameter, sourceType);
                }

            }
            return metadata;
        }

        public static SubscribeCompiler Subscribe { get; } = new SubscribeCompiler();

        public static ResolveCompiler Resolve { get; } = new ResolveCompiler();
    }
}
