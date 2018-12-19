using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration
{
    internal interface ITypeRegistry
    {
        void RegisterType(INamedType namedType);

        void RegisterType(INamedType namedType, ITypeBinding typeBinding);

        void RegisterType(TypeReference typeReference);

        void RegisterResolverType(Type resolverType);

        T GetType<T>(NameString typeName) where T : IType;

        T GetType<T>(TypeReference typeReference) where T : IType;

        bool TryGetType<T>(NameString typeName, out T type) where T : IType;

        bool TryGetType<T>(TypeReference typeReference, out T type)
            where T : IType;

        /// <summary>
        /// Gets all registered types.
        /// </summary>
        /// <returns>
        /// Returns all registered types.
        /// </returns>
        IEnumerable<INamedType> GetTypes();

        /// <summary>
        /// Gets all the type dependencies that do not have
        /// an associated schema type.
        /// </summary>
        /// <returns>
        /// Returns type dependencies that do not have
        /// an associated schema type.
        /// </returns>
        IEnumerable<TypeReference> GetUnresolvedTypes();

        bool TryGetTypeBinding<T>(NameString typeName, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(INamedType namedType, out T typeBinding)
            where T : ITypeBinding;
        bool TryGetTypeBinding<T>(Type clrType, out T typeBinding)
            where T : ITypeBinding;

        IEnumerable<ITypeBinding> GetTypeBindings();

        IEnumerable<Type> GetResolverTypes(NameString typeName);
    }

    internal static class TypeRegistryExtensions
    {
        public static bool TryGetObjectTypeField(
            this ITypeRegistry typeRegistry,
            IFieldReference fieldReference,
            out ObjectField field)
        {
            field = null;
            return typeRegistry.TryGetType(
                    fieldReference.TypeName, out ObjectType ot)
                && ot.Fields.TryGetField(
                    fieldReference.FieldName, out field);
        }
    }
}
