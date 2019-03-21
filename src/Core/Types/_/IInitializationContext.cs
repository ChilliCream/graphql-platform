using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public interface IInitializationContext
        : ITypeSystemObjectContext
    {
        void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind);

        void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind);

        void RegisterDependency(IDirectiveReference reference);

        void RegisterDependencyRange(
            IEnumerable<IDirectiveReference> references);

        void RegisterResolver(
            NameString fieldName,
            MemberInfo member,
            Type sourceType,
            Type resolverType);
    }
}
