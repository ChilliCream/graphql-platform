using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class TypeRegistry
        : ITypeRegistry
    {
        public new T GetType<T>(string typeName) where T : IType
        {
            throw new NotImplementedException();
        }

        public new T GetType<T>(Type nativeType) where T : IType
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITypeBinding> GetTypeBindings()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<INamedType> GetTypes()
        {
            throw new NotImplementedException();
        }

        public void RegisterType(INamedType namedType, ITypeBinding typeBinding = null)
        {
            throw new NotImplementedException();
        }

        public void RegisterType(Type nativeType)
        {
            throw new NotImplementedException();
        }

        public bool TryGetType<T>(string typeName, out T type) where T : IType
        {
            throw new NotImplementedException();
        }

        public bool TryGetTypeBinding<T>(string typeName, out T typeBinding) where T : ITypeBinding
        {
            throw new NotImplementedException();
        }

        public bool TryGetTypeBinding<T>(INamedType namedType, out T typeBinding) where T : ITypeBinding
        {
            throw new NotImplementedException();
        }

        public bool TryGetTypeBinding<T>(Type nativeType, out T typeBinding) where T : ITypeBinding
        {
            throw new NotImplementedException();
        }
    }
}
