using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal sealed class InitializationContext
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

        public IDictionary<FieldReference, IList<FieldMiddleware>> Components
        { get; } = new Dictionary<FieldReference, IList<FieldMiddleware>>();

        public IDictionary<FieldReference, RegisteredResolver> Resolvers
        { get; } = new Dictionary<FieldReference, RegisteredResolver>();

        public ICollection<ISchemaError> Errors { get; } =
            new List<ISchemaError>();

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
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            FieldReference normalized = Normalize(reference);

            if (!Components.TryGetValue(normalized,
                out IList<FieldMiddleware> comps))
            {
                comps = new List<FieldMiddleware>();
                Components[normalized] = comps;
            }

            foreach (FieldMiddleware component in components)
            {
                comps.Add(component);
            }
        }

        public void RegisterResolver(
            IFieldReference reference,
            Type sourceType,
            Type resolverType)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            Resolvers[Normalize(reference)] = resolverType == null
                ? new RegisteredResolver(sourceType, reference)
                : new RegisteredResolver(resolverType, sourceType, reference);
        }

        public void ReportError(ISchemaError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors.Add(error);
        }

        private static FieldReference Normalize(IFieldReference reference) =>
            new FieldReference(reference.TypeName, reference.FieldName);
    }
}
