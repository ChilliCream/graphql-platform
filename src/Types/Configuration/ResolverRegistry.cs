using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal class ResolverRegistry
        : IResolverRegistry
    {
        private readonly Dictionary<FieldReference, FieldDelegate> _resolvers =
            new Dictionary<FieldReference, FieldDelegate>();
        private readonly Dictionary<FieldReference, IFieldReference> _resolverBindings =
            new Dictionary<FieldReference, IFieldReference>();
        private readonly Dictionary<FieldReference, IFieldResolverDescriptor> _resolverDescriptors =
            new Dictionary<FieldReference, IFieldResolverDescriptor>();
        private readonly Dictionary<string, IDirectiveMiddleware> _middlewares =
            new Dictionary<string, IDirectiveMiddleware>();

        private readonly List<FieldMiddleware> _fieldMiddlewareComponents =
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

        public FieldDelegate GetResolver(
            NameString typeName,
            NameString fieldName)
        {
            var fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference, out var resolver))
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

        private IEnumerable<DirectiveMiddlewareDescriptor> CreateMiddlewareDescriptors()
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

            _fieldMiddlewareComponents.Add(middleware);
        }

        public FieldDelegate CreateMiddleware(
            FieldDelegate fieldResolver)
        {
            if (_fieldMiddlewareComponents.Count == 0)
            {
                return fieldResolver;
            }

            return BuildMiddleware(_fieldMiddlewareComponents, fieldResolver);
        }

        private FieldDelegate BuildMiddleware(
            IEnumerable<FieldMiddleware> components,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            foreach (FieldMiddleware component in components.Reverse())
            {
                next = component(next);
            }

            return next;
        }
    }
}
