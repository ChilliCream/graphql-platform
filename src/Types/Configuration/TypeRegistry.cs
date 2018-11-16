using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using HotChocolate.Runtime;
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
        private readonly Dictionary<Type, string> _clrTypeToSchemaType =
            new Dictionary<Type, string>();
        private readonly Dictionary<Type, HashSet<string>> _clrTypes =
            new Dictionary<Type, HashSet<string>>();
        private readonly HashSet<TypeReference> _unresolvedTypes =
            new HashSet<TypeReference>();
        private readonly ServiceFactory _serviceFactory;
        private bool _sealed;

        public TypeRegistry(ServiceFactory serviceFactory)
        {
            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }

            _serviceFactory = serviceFactory;
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
            if (!_clrTypes.TryGetValue(type, out HashSet<string> types))
            {
                types = new HashSet<string>();
                _clrTypes[type] = types;
            }
            types.Add(namedTypeName);
        }
    }
}
