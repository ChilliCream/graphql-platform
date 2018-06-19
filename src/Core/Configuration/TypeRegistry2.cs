using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry2
        : ITypeRegistry2
    {
        private readonly object _sync = new object();
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly Dictionary<string, INamedType> _namedTypes =
            new Dictionary<string, INamedType>();
        private readonly Dictionary<string, ITypeBinding> _typeBindings =
            new Dictionary<string, ITypeBinding>();
        private readonly Dictionary<Type, INamedType> _dotnetTypeToSchemaType =
            new Dictionary<Type, INamedType>();
        private readonly Dictionary<Type, INamedType> _nativeTypes =
            new Dictionary<Type, INamedType>();
        private readonly IServiceProvider _serviceProvider;

        public TypeRegistry2(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }
    }
}
