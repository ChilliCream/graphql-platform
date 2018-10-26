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

        bool IsDirective { get; }

        bool IsQueryType { get; }

        void RegisterType(
            INamedType namedType,
            ITypeBinding typeBinding = null);

        void RegisterType(TypeReference typeReference);

        void RegisterResolver(
            Type sourceType,
            Type resolverType,
            string fieldName,
            MemberInfo fieldMember);

        void RegisterMiddleware(IDirectiveMiddleware middleware);

        AsyncFieldResolverDelegate GetResolver(string fieldName);

        IDirectiveMiddleware GetMiddleware(string directiveName);

        T GetType<T>(TypeReference typeReference) where T : IType;

        DirectiveType GetDirectiveType(DirectiveReference directiveReference);

        IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType);

        bool TryGetNativeType(INamedType namedType, out Type nativeType);

        bool TryGetProperty<T>(
            INamedType namedType, string fieldName, out T member)
            where T : MemberInfo;

        void ReportError(SchemaError error);
    }
}
