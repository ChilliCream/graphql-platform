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
        private readonly Dictionary<NameString, INamedType> _namedTypes =
            new Dictionary<NameString, INamedType>();
        private readonly Dictionary<NameString, ITypeBinding> _typeBindings =
            new Dictionary<NameString, ITypeBinding>();
        private readonly Dictionary<Type, NameString> _clrTypeToSchemaType =
            new Dictionary<Type, NameString>();
        private readonly Dictionary<Type, HashSet<NameString>> _clrTypes =
            new Dictionary<Type, HashSet<NameString>>();
        private readonly HashSet<TypeReference> _unresolvedTypes =
            new HashSet<TypeReference>();
        private readonly HashSet<Type> _resolverTypes =
            new HashSet<Type>();
        private readonly Dictionary<NameString, List<Type>> _resolverTypeDict =
            new Dictionary<NameString, List<Type>>();
        private readonly ServiceFactory _serviceFactory;
        private bool _sealed;

        public TypeRegistry(ServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory
                ?? throw new ArgumentNullException(nameof(serviceFactory));
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
                if (typeBinding.Type != null
                    && _namedTypes.TryGetValue(
                        typeBinding.Name,
                        out INamedType namedType))
                {
                    AddNativeTypeBinding(typeBinding.Type, namedType.Name);
                }
            }
        }

        private void AddNativeTypeBinding(Type type, NameString namedTypeName)
        {
            if (type == typeof(object))
            {
                return;
            }

            if (!_clrTypes.TryGetValue(type, out HashSet<NameString> types))
            {
                types = new HashSet<NameString>();
                _clrTypes[type] = types;
            }
            types.Add(namedTypeName);
        }
    }
}
