using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal class ResolverRegistry
        : IResolverRegistry
    {
        private readonly Dictionary<FieldReference, FieldResolverDelegate>
            _resolvers =
                new Dictionary<FieldReference, FieldResolverDelegate>();
        private readonly Dictionary<FieldReference, IFieldReference>
            _resolverBindings =
                new Dictionary<FieldReference, IFieldReference>();
        private readonly Dictionary<FieldReference, IFieldResolverDescriptor>
            _resolverDescriptors =
                new Dictionary<FieldReference, IFieldResolverDescriptor>();
        private readonly Dictionary<string, IDirectiveMiddleware>
            _middlewares = new Dictionary<string, IDirectiveMiddleware>();

        private readonly List<FieldMiddleware> _middlewareComponents =
            new List<FieldMiddleware>();

        public void RegisterResolver(IFieldReference resolverBinding)
        {
            if (resolverBinding == null)
            {
                throw new ArgumentNullException(nameof(resolverBinding));
            }

            var fieldReference = new FieldReference(
                resolverBinding.TypeName, resolverBinding.FieldName);

            _resolverBindings[fieldReference] = resolverBinding;
        }

        public void RegisterResolver(
            IFieldResolverDescriptor resolverDescriptor)
        {
            if (resolverDescriptor == null)
            {
                throw new ArgumentNullException(nameof(resolverDescriptor));
            }

            _resolverDescriptors[resolverDescriptor.Field.ToFieldReference()] =
                resolverDescriptor;
        }

        public void RegisterMiddleware(IDirectiveMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewares[middleware.DirectiveName] = middleware;
        }

        public bool ContainsResolver(FieldReference fieldReference)
        {
            if (fieldReference == null)
            {
                throw new ArgumentNullException(nameof(fieldReference));
            }

            return _resolverDescriptors.ContainsKey(fieldReference)
                || _resolverBindings.ContainsKey(fieldReference);
        }

        public FieldResolverDelegate GetResolver(
            NameString typeName,
            NameString fieldName)
        {
            var fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference,
                out FieldResolverDelegate resolver))
            {
                return resolver;
            }
            return null;
        }

        public IDirectiveMiddleware GetMiddleware(string directiveName)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            if (_middlewares.TryGetValue(directiveName,
                out IDirectiveMiddleware middleware))
            {
                return middleware;
            }

            return null;
        }

        internal void BuildResolvers()
        {
            ResolverBuilderResult result = CompileResolvers();
            CompleteResolvers(result.Resolvers);
            CompleteMiddlewares(result.Middlewares);
        }

        private void CompleteResolvers(IEnumerable<FieldResolver> resolvers)
        {
            var fieldResolvers = new List<FieldResolver>();

            fieldResolvers.AddRange(resolvers);
            fieldResolvers.AddRange(_resolverBindings.Values
                .OfType<FieldResolver>());

            foreach (FieldResolver resolver in fieldResolvers)
            {
                _resolvers[resolver.ToFieldReference()] = resolver.Resolver;
            }
        }

        private void CompleteMiddlewares(
            IEnumerable<IDirectiveMiddleware> middlewares)
        {
            foreach (IDirectiveMiddleware middleware in middlewares)
            {
                _middlewares[middleware.DirectiveName] = middleware;
            }
        }

        private ResolverBuilderResult CompileResolvers()
        {
            var resolverBuilder = new ResolverBuilder();

            resolverBuilder.AddDescriptors(CreateResolverDescriptors());
            resolverBuilder.AddDescriptors(CreateMiddlewareDescriptors());
            resolverBuilder.AddDescriptors(_resolverDescriptors.Values);

            return resolverBuilder.Build();
        }

        private IEnumerable<SourceResolverDescriptor> CreateResolverDescriptors()
        {
            foreach (FieldMember binding in _resolverBindings.Values
                .OfType<FieldMember>())
            {
                yield return new SourceResolverDescriptor(binding);
            }
        }

        private IEnumerable<DirectiveMiddlewareDescriptor>
            CreateMiddlewareDescriptors()
        {
            foreach (DirectiveMethodMiddleware methodMiddleware in
                _middlewares.Values.OfType<DirectiveMethodMiddleware>())
            {
                yield return new DirectiveMiddlewareDescriptor(
                    methodMiddleware);
            }
        }

        public void RegisterMiddleware(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareComponents.Add(middleware);
        }

        public FieldDelegate CreateMiddleware(
            IEnumerable<FieldMiddleware> middlewareComponents,
            FieldResolverDelegate fieldResolver,
            bool isIntrospection)
        {
            if (middlewareComponents == null)
            {
                throw new ArgumentNullException(nameof(middlewareComponents));
            }

            FieldMiddleware[] components = middlewareComponents.ToArray();

            if (isIntrospection
                || (_middlewareComponents.Count == 0
                    && components.Length == 0))
            {
                if (fieldResolver == null)
                {
                    return null;
                }
                return CreateResolverMiddleware(fieldResolver);
            }

            return BuildMiddleware(
                _middlewareComponents,
                components,
                fieldResolver);
        }

        private static FieldDelegate BuildMiddleware(
            IReadOnlyList<FieldMiddleware> components,
            IReadOnlyList<FieldMiddleware> mappedComponents,
            FieldResolverDelegate fieldResolver)
        {
            return IntegrateComponents(components,
                IntegrateComponents(mappedComponents,
                    CreateResolverMiddleware(fieldResolver)));
        }

        private static FieldDelegate IntegrateComponents(
            IReadOnlyList<FieldMiddleware> components,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                next = components[i].Invoke(next);
            }

            return next;
        }

        private static FieldDelegate CreateResolverMiddleware(
            FieldResolverDelegate fieldResolver)
        {
            return async ctx =>
            {
                if (!ctx.IsResultModified && fieldResolver != null)
                {
                    ctx.Result = await fieldResolver.Invoke(ctx)
                        .ConfigureAwait(false);
                }
            };
        }
    }
}
