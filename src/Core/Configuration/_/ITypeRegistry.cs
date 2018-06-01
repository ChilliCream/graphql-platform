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

        IEnumerable<ITypeBinding> GetTypeBindings();
    }

    internal static class TypeRegistryExtensions
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

        public static IOutputType GetOutputType(
            this ITypeRegistry typeRegistry, ITypeNode typeNode)
        {
            IType type = GetType(typeRegistry, typeNode);
            if (type is IOutputType outputType)
            {
                return outputType;
            }

            throw new ArgumentException(
                "The specified type is not an output type.",
                nameof(typeNode));
        }

        public static IInputType GetInputType(
            this ITypeRegistry typeRegistry, ITypeNode typeNode)
        {
            IType type = GetType(typeRegistry, typeNode);
            if (type is IInputType inputType)
            {
                return inputType;
            }

            throw new ArgumentException(
                "The specified type is not an output type.",
                nameof(typeNode));
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
