using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal class DependencyDescriptor<T>
        : IDependencyDescriptor
        where T : DefinitionBase
    {
        private readonly TypeConfiguration<T> _configuration;

        public DependencyDescriptor(TypeConfiguration<T> configuration)
        {
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IDependencyDescriptor DependsOn<TType>()
            where TType : ITypeSystem =>
            DependsOn<TType>(false);

        public IDependencyDescriptor DependsOn<TType>(bool mustBeNamed)
            where TType : ITypeSystem
        {
            TypeDependencyKind kind = mustBeNamed
                ? TypeDependencyKind.Named
                : TypeDependencyKind.Default;

            _configuration.Dependencies.Add(
                TypeDependency.FromSchemaType(
                    typeof(TType), kind));

            return this;
        }

        public IDependencyDescriptor DependsOn(
            NameString typeName) =>
            DependsOn(typeName, false);

        public IDependencyDescriptor DependsOn(
            NameString typeName,
            bool mustBeNamed)
        {
            typeName.EnsureNotEmpty(nameof(typeName));

            TypeDependencyKind kind = mustBeNamed
                ? TypeDependencyKind.Named
                : TypeDependencyKind.Default;

            _configuration.Dependencies.Add(
                new TypeDependency(
                    new SyntaxTypeReference(
                        new NamedTypeNode(typeName), TypeContext.None),
                    kind));

            return this;
        }
    }
}
