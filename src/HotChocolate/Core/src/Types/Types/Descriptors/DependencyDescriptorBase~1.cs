using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

internal abstract class DependencyDescriptorBase
{
    private readonly ITypeSystemConfigurationTask _configuration;

    protected DependencyDescriptorBase(
        ITypeInspector typeInspector,
        ITypeSystemConfigurationTask configuration)
    {
        TypeInspector = typeInspector ??
            throw new ArgumentNullException(nameof(typeInspector));
        _configuration = configuration ??
            throw new ArgumentNullException(nameof(configuration));
    }

    protected ITypeInspector TypeInspector { get; }

    protected abstract TypeDependencyFulfilled DependencyFulfilled { get; }

    protected void DependsOn<TType>(bool mustBeNamedOrCompleted)
        where TType : ITypeSystemMember =>
        DependsOn(typeof(TType), mustBeNamedOrCompleted);

    protected void DependsOn(Type schemaType, bool mustBeNamedOrCompleted) =>
        DependsOn(TypeInspector.GetType(schemaType), mustBeNamedOrCompleted);

    protected void DependsOn(IExtendedType schemaType, bool mustBeNamedOrCompleted)
    {
        ArgumentNullException.ThrowIfNull(schemaType);

        if (!schemaType.IsSchemaType)
        {
            throw new ArgumentException(
                TypeResources.DependencyDescriptorBase_OnlyTsoIsAllowed,
                nameof(schemaType));
        }

        var kind = mustBeNamedOrCompleted
            ? DependencyFulfilled
            : TypeDependencyFulfilled.Default;

        _configuration.AddDependency(
            TypeDependency.FromSchemaType(schemaType, kind));
    }

    protected void DependsOn(
        string typeName,
        bool mustBeNamedOrCompleted)
    {
        typeName.EnsureGraphQLName();

        var kind = mustBeNamedOrCompleted
            ? DependencyFulfilled
            : TypeDependencyFulfilled.Default;

        _configuration.AddDependency(
            new TypeDependency(
                TypeReference.Create(new NamedTypeNode(typeName)),
                kind));
    }

    protected void DependsOn(
        TypeReference typeReference,
        bool mustBeNamedOrCompleted)
    {
        ArgumentNullException.ThrowIfNull(typeReference);

        var kind = mustBeNamedOrCompleted
            ? DependencyFulfilled
            : TypeDependencyFulfilled.Default;

        _configuration.AddDependency(new TypeDependency(typeReference, kind));
    }
}
