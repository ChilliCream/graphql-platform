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
        private readonly Dictionary<FieldReference, FieldResolverDelegate> _resolvers =
            new Dictionary<FieldReference, FieldResolverDelegate>();
        private readonly Dictionary<FieldReference, IFieldReference> _resolverBindings =
            new Dictionary<FieldReference, IFieldReference>();
        private readonly Dictionary<FieldReference, IFieldResolverDescriptor> _resolverDescriptors =
            new Dictionary<FieldReference, IFieldResolverDescriptor>();
        private readonly Dictionary<DirectiveMiddlewareReference, IDirectiveMiddleware> _middlewares =
            new Dictionary<DirectiveMiddlewareReference, IDirectiveMiddleware>();

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

            _middlewares[CreateReference(middleware)] = middleware;
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

        public FieldResolverDelegate GetResolver(string typeName, string fieldName)
        {
            var fieldReference = new FieldReference(typeName, fieldName);
            if (_resolvers.TryGetValue(fieldReference, out FieldResolverDelegate resolver))
            {
                return resolver;
            }
            return null;
        }

        public IDirectiveMiddleware GetMiddleware(
            string directiveName,
            MiddlewareKind kind)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            if (_middlewares.TryGetValue(
                CreateReference(directiveName, kind),
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
                _middlewares[CreateReference(middleware)] = middleware;
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
                yield return new DirectiveMiddlewareDescriptor(methodMiddleware);
            }
        }

        private static DirectiveMiddlewareReference CreateReference(
            IDirectiveMiddleware middleware)
        {
            return new DirectiveMiddlewareReference(
                middleware.DirectiveName,
                middleware.Kind);
        }

        private static DirectiveMiddlewareReference CreateReference(
            string directiveName, MiddlewareKind kind)
        {
            return new DirectiveMiddlewareReference(directiveName, kind);
        }
    }
}
