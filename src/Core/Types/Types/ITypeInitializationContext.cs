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

        IServiceProvider Services { get; }

        void RegisterType(
            INamedType namedType,
            ITypeBinding typeBinding = null);

        void RegisterType(TypeReference typeReference);

        void RegisterResolver(
            Type sourceType,
            Type resolverType,
            NameString fieldName,
            MemberInfo fieldMember);

        void RegisterMiddleware(IDirectiveMiddleware middleware);

        FieldResolverDelegate GetResolver(NameString fieldName);

        IEnumerable<Type> GetResolverTypes(NameString typeName);

        IDirectiveMiddleware GetMiddleware(string directiveName);

        FieldResolverDelegate CreateFieldMiddleware(
            IEnumerable<FieldMiddleware> mappedMiddlewareComponents,
            FieldResolverDelegate fieldResolver);

        T GetType<T>(TypeReference typeReference) where T : IType;

        DirectiveType GetDirectiveType(DirectiveReference directiveReference);

        IReadOnlyCollection<ObjectType> GetPossibleTypes(
            INamedType abstractType);

        bool TryGetNativeType(INamedType namedType, out Type nativeType);

        bool TryGetProperty<T>(
            INamedType namedType, NameString fieldName, out T member)
            where T : MemberInfo;

        void ReportError(SchemaError error);
    }
}
