using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
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

    internal class InitializationContext
        : IInitializationContext
    {
        private readonly List<TypeDependency> _typeDependencies =
            new List<TypeDependency>();
        private readonly List<IDirectiveReference> _directiveReferences =
            new List<IDirectiveReference>();

        public InitializationContext(
            ITypeSystemObject type,
            IServiceProvider services)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Services = services
                ?? throw new ArgumentNullException(nameof(services));

            IsDirective = type is DirectiveType;

            if (type is INamedType nt)
            {
                IsType = true;
                IsIntrospectionType = nt.IsIntrospectionType();
            }
        }

        public ITypeSystemObject Type { get; }

        public bool IsType { get; }

        public bool IsIntrospectionType { get; }

        public bool IsDirective { get; }

        public IServiceProvider Services { get; }

        public ICollection<TypeDependency> TypeDependencies =>
            _typeDependencies;

        public ICollection<IDirectiveReference> DirectiveReferences =>
            _directiveReferences;

        public void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _typeDependencies.Add(new TypeDependency(reference, kind));
        }

        public void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            foreach (ITypeReference reference in references)
            {
                _typeDependencies.Add(new TypeDependency(reference, kind));
            }
        }

        public void RegisterDependency(IDirectiveReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _directiveReferences.Add(reference);
        }

        public void RegisterMiddleware(
            IFieldReference reference,
            IEnumerable<FieldMiddleware> components)
        {
            throw new NotImplementedException();
        }

        public void RegisterResolver(
            IFieldReference reference,
            Type sourceType,
            Type resolverType)
        {
            throw new NotImplementedException();
        }

        public void ReportError(ISchemaError error)
        {
            throw new NotImplementedException();
        }
    }
}
