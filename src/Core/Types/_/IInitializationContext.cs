using System;
using System.Collections.Generic;
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

        void RegisterResolver(
            IFieldReference reference,
            Type sourceType,
            Type resolverType);

        void RegisterMiddleware(
            IFieldReference reference,
            IEnumerable<FieldMiddleware> components);
    }
}
