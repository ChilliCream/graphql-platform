using System;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors;

internal abstract class DependencyDescriptorBase
{
    private readonly ITypeSystemMemberConfiguration _configuration;

    protected DependencyDescriptorBase(
        ITypeInspector typeInspector,
        ITypeSystemMemberConfiguration configuration)
    {
        TypeInspector = typeInspector ??
            throw new ArgumentNullException(nameof(typeInspector));
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

        _configuration.AddDependency(
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

        _configuration.AddDependency(
            new TypeDependency(
                TypeReference.Create(new NamedTypeNode(typeName), TypeContext.None),
                kind));
    }
}
