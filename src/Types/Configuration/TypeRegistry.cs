using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry
        : ITypeRegistry
    {
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly Dictionary<string, INamedType> _namedTypes =
            new Dictionary<string, INamedType>();
        private readonly Dictionary<string, ITypeBinding> _typeBindings =
            new Dictionary<string, ITypeBinding>();
        private readonly Dictionary<Type, string> _dotnetTypeToSchemaType =
            new Dictionary<Type, string>();
        private readonly Dictionary<Type, HashSet<string>> _nativeTypes =
            new Dictionary<Type, HashSet<string>>();
        private readonly IServiceProvider _serviceProvider;
        private bool _sealed;

        public TypeRegistry(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }

        public void CompleteRegistartion()
        {
            if (!_sealed)
            {
                CreateNativeTypeLookup();
                _sealed = true;
            }
        }

        private void CreateNativeTypeLookup()
        {
            foreach (ITypeBinding typeBinding in _typeBindings.Values)
            {
                if (typeBinding.Type != null && _namedTypes.TryGetValue(
                    typeBinding.Name, out INamedType namedType))
                {
                    AddNativeTypeBinding(typeBinding.Type, namedType.Name);
                }
            }
        }

        private void AddNativeTypeBinding(Type type, string namedTypeName)
        {
            if (!_nativeTypes.TryGetValue(type, out HashSet<string> types))
            {
                types = new HashSet<string>();
                _nativeTypes[type] = types;
            }
            types.Add(namedTypeName);
        }

    }
}
