using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface ITypeInitializationContext
    {
        IHasName Type { get; }

        bool IsDirective { get; }

        bool IsQueryType { get; }

        IServiceProvider Services { get; }

        IsOfTypeFallback IsOfType { get; }

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

        IDirectiveMiddleware GetMiddleware(NameString directiveName);

        FieldDelegate CreateMiddleware(
            IEnumerable<FieldMiddleware> middlewareComponents,
            FieldResolverDelegate fieldResolver,
            bool isIntrospection);

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
