using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface ITypeInitializationContext
    {
        INamedType Type { get; }

        void RegisterType(INamedType namedType, ITypeBinding typeBinding = null);

        void RegisterType(TypeReference typeReference);

        void RegisterResolver(string fieldName, MemberInfo fieldMember);

        FieldResolverDelegate GetResolver(string fieldName);

        T GetType<T>(TypeReference typeReference) where T : IType;

        IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType);

        bool TryGetNativeType(INamedType namedType, out Type nativeType);

        bool TryGetProperty<T>(INamedType namedType, string fieldName, out T member)
            where T : MemberInfo;

        void ReportError(SchemaError error);
    }
}
