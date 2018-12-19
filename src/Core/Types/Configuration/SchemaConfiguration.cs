using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly List<ResolverBindingInfo> _resolverBindings =
            new List<ResolverBindingInfo>();
        private readonly List<TypeBindingInfo> _typeBindings =
            new List<TypeBindingInfo>();
        private readonly Action<IServiceProvider> _registerServiceProvider;
        private readonly ITypeRegistry _typeRegistry;
        private readonly IResolverRegistry _resolverRegistry;
        private readonly IDirectiveRegistry _directiveRegistry;

        public SchemaConfiguration(
            Action<IServiceProvider> registerServiceProvider,
            ITypeRegistry typeRegistry,
            IResolverRegistry resolverRegistry,
            IDirectiveRegistry directiveRegistry)
        {
            _registerServiceProvider = registerServiceProvider
                ?? throw new ArgumentNullException(
                        nameof(registerServiceProvider));
            _typeRegistry = typeRegistry
                ?? throw new ArgumentNullException(nameof(typeRegistry));
            _resolverRegistry = resolverRegistry
                ?? throw new ArgumentNullException(nameof(resolverRegistry));
            _directiveRegistry = directiveRegistry
                ?? throw new ArgumentNullException(nameof(directiveRegistry));
        }

        public ISchemaOptions Options { get; set; } = new SchemaOptions();

        internal IReadOnlyCollection<TypeBindingInfo> TypeBindings =>
            _typeBindings;

        internal IReadOnlyCollection<ResolverBindingInfo> ResolverBindings =>
            _resolverBindings;

        public void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            _registerServiceProvider(serviceProvider);
        }

        public IMiddlewareConfiguration Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _resolverRegistry.RegisterMiddleware(middleware);
            return this;
        }
    }
}
