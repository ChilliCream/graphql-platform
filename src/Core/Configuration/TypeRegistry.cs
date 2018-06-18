using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class TypeRegistry
        : ITypeRegistry
    {
        private Dictionary<string, INamedType> _namedTypes = new Dictionary<string, INamedType>();
        private Dictionary<string, ITypeBinding> _typeBindings = new Dictionary<string, ITypeBinding>();
        private Dictionary<Type, INamedType> _typesToNamedTypes = new Dictionary<Type, INamedType>();
        private readonly IServiceProvider _serviceProvider;

        public TypeRegistry(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            _serviceProvider = serviceProvider;
        }

        public void RegisterType(INamedType namedType, ITypeBinding typeBinding = null)
        {
            if (namedType == null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            if (!_namedTypes.ContainsKey(namedType.Name))
            {
                _namedTypes[namedType.Name] = namedType;
            }

            Type nativeNamedType = namedType.GetType();
            if (!_typesToNamedTypes.ContainsKey(nativeNamedType)
                && !BaseTypes.IsNonGenericBaseType(nativeNamedType))
            {
                _typesToNamedTypes[nativeNamedType] = namedType;
            }

            if (typeBinding != null)
            {
                _typeBindings[namedType.Name] = typeBinding;
            }
        }

        public void RegisterType(Type nativeNamedType)
        {
            if (nativeNamedType == null)
            {
                throw new ArgumentNullException(nameof(nativeNamedType));
            }

            if (BaseTypes.IsNonGenericBaseType(nativeNamedType))
            {
                throw new ArgumentException(
                    $"The {nativeNamedType.GetTypeName()} type must be defined explicit",
                    nameof(nativeNamedType));
            }

            if (!_typesToNamedTypes.ContainsKey(nativeNamedType))
            {
                if (!_typesToNamedTypes.ContainsKey(nativeNamedType))
                {
                    INamedType namedType = (INamedType)_serviceProvider
                        .GetService(nativeNamedType);
                    RegisterType(namedType);
                }
            }
        }

        public void RegisterType(ITypeNode type)
        {

        }

         private string ExtractTypeName(ITypeNode typeNode)
        {
            ITypeNode current = typeNode;
            for (int i = 0; i < 4; i++)
            {
                if (current.Kind == NodeKind.NonNullType)
                {
                    current = ((NonNullTypeNode)current).Type;
                }

                if (current.Kind == NodeKind.ListType)
                {
                    current = ((ListTypeNode)current).Type;
                }

                if (current.Kind == NodeKind.NamedType)
                {
                    return ((NamedTypeNode)current).Name.Value;
                }
            }
            return null;
        }

        public T GetType<T>(string typeName) where T : IType
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_namedTypes.TryGetValue(typeName, out INamedType namedType)
                && namedType is T t)
            {
                return t;
            }

            throw new ArgumentException(
                "The specified type does not exist or " +
                "is not of the specified kind.",
                nameof(typeName));
        }

        public T GetType<T>(Type nativeNamedType) where T : IType
        {
            if (nativeNamedType == null)
            {
                throw new ArgumentNullException(nameof(nativeNamedType));
            }

            if (_typesToNamedTypes.TryGetValue(nativeNamedType, out INamedType namedType)
                && namedType is T t)
            {
                return t;
            }

            throw new ArgumentException(
                $"The {nativeNamedType.GetTypeName()} type does not exist or " +
                "is not of the specified kind.",
                nameof(nativeNamedType));
        }

        public bool TryGetType<T>(string typeName, out T type) where T : IType
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (_namedTypes.TryGetValue(typeName, out INamedType namedType)
                && namedType is T t)
            {
                type = t;
                return true;
            }

            type = default(T);
            return false;
        }

        public IEnumerable<INamedType> GetTypes()
        {
            return _namedTypes.Values;
        }

        public bool TryGetTypeBinding<T>(string typeName, out T typeBinding)
            where T : ITypeBinding
        {
            if (_typeBindings.TryGetValue(typeName, out ITypeBinding binding)
                && binding is T b)
            {
                typeBinding = b;
                return true;
            }

            typeBinding = default(T);
            return false;
        }

        public bool TryGetTypeBinding<T>(INamedType namedType, out T typeBinding)
            where T : ITypeBinding
        {
            return TryGetTypeBinding(namedType.Name, out typeBinding);
        }

        public bool TryGetTypeBinding<T>(Type nativeType, out T typeBinding)
            where T : ITypeBinding
        {
            if (_typesToNamedTypes.TryGetValue(nativeType, out INamedType namedType))
            {
                return TryGetTypeBinding(namedType, out typeBinding);
            }

            typeBinding = default(T);
            return false;
        }

        public IEnumerable<ITypeBinding> GetTypeBindings()
        {
            return _typeBindings.Values;
        }
    }
}
