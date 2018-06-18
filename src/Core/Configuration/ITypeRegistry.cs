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
        void RegisterType(Type nativeType);
        void RegisterType(ITypeNode type);

        T GetType<T>(string typeName) where T : IType;
        T GetType<T>(Type nativeNamedType) where T : IType;

        bool TryGetType<T>(string typeName, out T type) where T : IType;

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
        public static void RegisterType(
            this ITypeRegistry typeRegistry,
            TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (typeReference.IsNativeTypeReference())
            {
                typeRegistry.RegisterType(typeReference.NativeType);
            }
            else
            {
                typeRegistry.RegisterType(typeReference.Type);
            }
        }

        public static T GetType<T>(
           this ITypeRegistry typeRegistry,
           TypeReference typeReference)
           where T : IType
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (typeReference.IsNativeTypeReference())
            {
                return typeRegistry.GetType<T>(typeReference.NativeType);
            }
            else
            {
                IType type = GetType(typeRegistry, typeReference.Type);
                if (type is T t)
                {
                    return t;
                }
                return default(T);
            }
        }

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

        public static IType GetType(
            this ITypeRegistry typeRegistry, ITypeNode typeNode)
        {
            if (typeNode.Kind == NodeKind.NonNullType)
            {
                return new NonNullType(
                    GetType(typeRegistry,
                        ((NonNullTypeNode)typeNode).Type));
            }

            if (typeNode.Kind == NodeKind.ListType)
            {
                return new ListType(GetType(
                    typeRegistry, ((ListTypeNode)typeNode).Type));
            }

            if (typeNode.Kind == NodeKind.NamedType)
            {
                return typeRegistry.GetType<IType>(
                    ((NamedTypeNode)typeNode).Name.Value);
            }

            throw new NotSupportedException();
        }
    }
}
