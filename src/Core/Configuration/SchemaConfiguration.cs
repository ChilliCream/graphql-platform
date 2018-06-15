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
        private readonly Dictionary<string, INamedType> _types =
            new Dictionary<string, INamedType>();
        private readonly List<ResolverBindingInfo> _resolverBindings =
            new List<ResolverBindingInfo>();
        private readonly List<TypeBindingInfo> _typeBindings =
            new List<TypeBindingInfo>();
        private readonly ServiceManager _serviceManager;

        public SchemaConfiguration(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager
                ?? throw new ArgumentNullException(nameof(serviceManager));
        }

        public ISchemaOptions Options { get; set; } = new SchemaOptions();
    }
}
