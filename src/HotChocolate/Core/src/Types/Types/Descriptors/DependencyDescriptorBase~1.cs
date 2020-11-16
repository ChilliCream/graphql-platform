using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    internal abstract class DependencyDescriptorBase<T>
        where T : DefinitionBase
    {
        private readonly TypeConfiguration<T> _configuration;

        protected DependencyDescriptorBase(
            ITypeInspector typeInspector,
            TypeConfiguration<T> configuration)
        {
            TypeInspector = typeInspector ??
                throw new ArgumentNullException(nameof(configuration));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
        }

        protected ITypeInspector TypeInspector { get; }

        protected abstract TypeDependencyKind DependencyKind { get; }

        protected void DependsOn<TType>(bool mustBeNamedOrCompleted)
            where TType : ITypeSystemMember =>
            DependsOn(typeof(TType), mustBeNamedOrCompleted);

        protected void DependsOn(Type schemaType, bool mustBeNamedOrCompleted) =>
            DependsOn(TypeInspector.GetType(schemaType), mustBeNamedOrCompleted);

        protected void DependsOn(IExtendedType schemaType, bool mustBeNamedOrCompleted)
        {
            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            if (!schemaType.IsSchemaType)
            {
                throw new ArgumentException(
                    TypeResources.DependencyDescriptorBase_OnlyTsoIsAllowed,
                    nameof(schemaType));
            }

            TypeDependencyKind kind = mustBeNamedOrCompleted
                ? DependencyKind
                : TypeDependencyKind.Default;

            _configuration.Dependencies.Add(
                TypeDependency.FromSchemaType(schemaType, kind));
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
                    TypeReference.Create(new NamedTypeNode(typeName), TypeContext.None),
                    kind));
        }
    }
}
