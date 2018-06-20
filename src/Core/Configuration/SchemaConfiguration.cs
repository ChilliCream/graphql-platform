using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;

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

        public void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            _registerServiceProvider(serviceProvider);
        }
    }
}
