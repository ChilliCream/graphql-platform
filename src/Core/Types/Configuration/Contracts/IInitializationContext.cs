using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    public interface IInitializationContext
        : ITypeSystemObjectContext
    {
        void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind);

        void RegisterDependency(
            TypeDependency dependency);

        void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind);

        void RegisterDependencyRange(
            IEnumerable<TypeDependency> dependencies);

        void RegisterDependency(IDirectiveReference reference);

        void RegisterDependencyRange(
            IEnumerable<IDirectiveReference> references);

        void RegisterResolver(
            NameString fieldName,
            MemberInfo member,
            Type sourceType,
            Type resolverType);

        void RegisterResolver(
            NameString fieldName,
            Expression expression,
            Type sourceType,
            Type resolverType);
    }
}
