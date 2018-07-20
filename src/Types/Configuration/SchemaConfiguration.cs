using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

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

        public SchemaConfiguration(
            Action<IServiceProvider> registerServiceProvider,
            ITypeRegistry typeRegistry)
        {
            _registerServiceProvider = registerServiceProvider
                ?? throw new ArgumentNullException(nameof(registerServiceProvider));
            _typeRegistry = typeRegistry
                ?? throw new ArgumentNullException(nameof(typeRegistry));
        }

        public ISchemaOptions Options { get; set; } = new SchemaOptions();

        internal IReadOnlyCollection<TypeBindingInfo> TypeBindings => _typeBindings;

        internal IReadOnlyCollection<ResolverBindingInfo> ResolverBindings => _resolverBindings;

        internal 

        public void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            _registerServiceProvider(serviceProvider);
        }
    }
}
