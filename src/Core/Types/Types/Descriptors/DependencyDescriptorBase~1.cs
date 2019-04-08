using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal abstract class DependencyDescriptorBase<T>
        where T : DefinitionBase
    {
        private readonly TypeConfiguration<T> _configuration;

        public DependencyDescriptorBase(TypeConfiguration<T> configuration)
        {
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected abstract TypeDependencyKind DependencyKind { get; }

        protected void DependsOn<TType>(bool mustBeNamedOrCompleted)
            where TType : ITypeSystem =>
            DependsOn(typeof(TType), mustBeNamedOrCompleted);

        protected void DependsOn(Type schemaType, bool mustBeNamedOrCompleted)
        {
            if (schemaType == null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            if (!typeof(ITypeSystem).IsAssignableFrom(schemaType))
            {
                // TODO : resources
                throw new ArgumentException(
                    "Only type system objects are allowed.");
            }

            TypeDependencyKind kind = mustBeNamedOrCompleted
                ? DependencyKind
                : TypeDependencyKind.Default;

            _configuration.Dependencies.Add(
                TypeDependency.FromSchemaType(
                    schemaType, kind));
        }

        protected void DependsOn(
            NameString typeName,
            bool mustBeNamedOrCompleted)
        {
            typeName.EnsureNotEmpty(nameof(typeName));

            TypeDependencyKind kind = mustBeNamedOrCompleted
                ? DependencyKind
                : TypeDependencyKind.Default;

            _configuration.Dependencies.Add(
                new TypeDependency(
                    new SyntaxTypeReference(
                        new NamedTypeNode(typeName), TypeContext.None),
                    kind));
        }
    }
}
