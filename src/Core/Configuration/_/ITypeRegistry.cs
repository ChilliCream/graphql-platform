using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface ITypeRegistry
    {
        void RegisterType(INamedType namedType, ITypeBinding typeBinding = null);
        void RegisterType(Type nativeType);

        T GetType<T>(string typeName) where T : IType;
        T GetType<T>(Type nativeType) where T : IType;

        bool TryGetType<T>(string typeName, out T type) where T : IType;

        IEnumerable<INamedType> GetTypes();

        bool TryGetTypeBinding<T>(string typeName, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(INamedType namedType, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(Type nativeType, out T typeBinding)
            where T : ITypeBinding;
    }

    public static class TypeRegistryExtensions
    {
        public static bool TryGetObjectTypeField(
            this ITypeRegistry typeRegistry,
            FieldReference fieldReference,
            out Field field)
        {
            field = null;
            return typeRegistry.TryGetType<ObjectType>(
                    fieldReference.TypeName, out ObjectType ot)
                && ot.Fields.TryGetValue(fieldReference.FieldName, out field);
        }
    }
}
