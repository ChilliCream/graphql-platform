using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface ITypeRegistry
    {
        void RegisterType(INamedType namedType, ITypeBinding typeBinding = null);
        void RegisterType(TypeReference typeReference);

        T GetType<T>(string typeName) where T : IType;
        T GetType<T>(TypeReference typeReference) where T : IType;

        bool TryGetType<T>(string typeName, out T type) where T : IType;
        bool TryGetType<T>(TypeReference typeReference, out T type) where T : IType;

        IEnumerable<INamedType> GetTypes();

        bool TryGetTypeBinding<T>(string typeName, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(INamedType namedType, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(Type nativeNamedType, out T typeBinding)
            where T : ITypeBinding;

        IEnumerable<ITypeBinding> GetTypeBindings();
    }

    internal static class TypeRegistryExtensions
    {
        public static bool TryGetObjectTypeField(
            this ITypeRegistry typeRegistry,
            FieldReference fieldReference,
            out ObjectField field)
        {
            field = null;
            return typeRegistry.TryGetType(fieldReference.TypeName, out ObjectType ot)
                && ot.Fields.TryGetField(fieldReference.FieldName, out field);
        }
    }
}
