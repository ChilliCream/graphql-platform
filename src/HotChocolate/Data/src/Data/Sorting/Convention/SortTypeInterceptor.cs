using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Sorting;

public sealed class SortTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, ISortConvention> _conventions = [];

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case SortInputTypeConfiguration inputDefinition:
                OnBeforeRegisteringDependencies(discoveryContext, inputDefinition);
                break;
            case SortEnumTypeConfiguration enumTypeDefinition:
                OnBeforeRegisteringDependencies(discoveryContext, enumTypeDefinition);
                break;
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case SortInputTypeConfiguration inputDefinition:
                OnBeforeCompleteName(completionContext, inputDefinition);
                break;
            case SortEnumTypeConfiguration enumTypeDefinition:
                OnBeforeCompleteName(completionContext, enumTypeDefinition);
                break;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case SortInputTypeConfiguration inputDefinition:
                OnBeforeCompleteType(completionContext, inputDefinition);
                break;
            case SortEnumTypeConfiguration enumTypeDefinition:
                OnBeforeCompleteType(completionContext, enumTypeDefinition);
                break;
        }
    }

    private void OnBeforeRegisteringDependencies(
        ITypeDiscoveryContext discoveryContext,
        SortInputTypeConfiguration configuration)
    {
        var convention =
            GetConvention(discoveryContext.DescriptorContext, configuration.Scope);

        var descriptor = SortInputTypeDescriptor.New(
            discoveryContext.DescriptorContext,
            configuration.EntityType!,
            configuration.Scope);

        var typeReference =
            TypeReference.Create(discoveryContext.Type, configuration.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        var extensionDefinition = descriptor.CreateConfiguration();

        discoveryContext.RegisterDependencies(extensionDefinition);
    }

    private void OnBeforeRegisteringDependencies(
        ITypeDiscoveryContext discoveryContext,
        SortEnumTypeConfiguration configuration)
    {
        var convention =
            GetConvention(discoveryContext.DescriptorContext, configuration.Scope);

        var descriptor = SortEnumTypeDescriptor.New(
            discoveryContext.DescriptorContext,
            configuration.EntityType,
            configuration.Scope);

        var typeReference =
            TypeReference.Create(discoveryContext.Type, configuration.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        var extensionDefinition = descriptor.CreateConfiguration();

        discoveryContext.RegisterDependencies(extensionDefinition);
    }

    private void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        SortInputTypeConfiguration configuration)
    {
        var convention =
            GetConvention(completionContext.DescriptorContext, configuration.Scope);

        var descriptor = SortInputTypeDescriptor.New(
            completionContext.DescriptorContext,
            configuration.EntityType!,
            configuration.Scope);

        var typeReference =
            TypeReference.Create(completionContext.Type, configuration.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper.MergeSortInputTypeDefinitions(
            completionContext,
            descriptor.CreateConfiguration(),
            configuration);

        if (!string.IsNullOrEmpty(configuration.Name)
            && configuration is IHasScope { Scope: not null })
        {
            configuration.Name = completionContext.Scope + "_" + configuration.Name;
        }
    }

    private void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        SortEnumTypeConfiguration configuration)
    {
        var convention =
            GetConvention(completionContext.DescriptorContext, configuration.Scope);

        var descriptor = SortEnumTypeDescriptor.New(
            completionContext.DescriptorContext,
            configuration.EntityType,
            configuration.Scope);

        var typeReference =
            TypeReference.Create(completionContext.Type, configuration.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper.MergeSortEnumTypeDefinitions(
            completionContext,
            descriptor.CreateConfiguration(),
            configuration);

        if (!string.IsNullOrEmpty(configuration.Name)
            && configuration is IHasScope { Scope: not null })
        {
            configuration.Name = completionContext.Scope + "_" + configuration.Name;
        }
    }

    private void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        SortInputTypeConfiguration configuration)
    {
        var convention = GetConvention(completionContext.DescriptorContext, configuration.Scope);

        foreach (var field in configuration.Fields)
        {
            if (field is SortFieldConfiguration sortFieldDefinition)
            {
                if (completionContext.TryPredictTypeKind(sortFieldDefinition.Type!, out var kind)
                    && kind != TypeKind.Enum)
                {
                    field.Type = field.Type!.With(scope: completionContext.Scope);
                }

                sortFieldDefinition.Metadata =
                    convention.CreateMetaData(completionContext, configuration, sortFieldDefinition);

                if (sortFieldDefinition.Handler is null)
                {
                    if (convention.TryGetFieldHandler(
                        completionContext,
                        configuration,
                        sortFieldDefinition,
                        out var handler))
                    {
                        sortFieldDefinition.Handler = handler;
                    }
                    else
                    {
                        throw SortInterceptor_NoFieldHandlerFoundForField(
                            configuration,
                            sortFieldDefinition);
                    }
                }
            }
        }
    }

    private void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        SortEnumTypeConfiguration configuration)
    {
        var convention =
            GetConvention(completionContext.DescriptorContext, completionContext.Scope);

        foreach (var enumValue in configuration.Values)
        {
            if (enumValue is SortEnumValueConfiguration sortEnumValueDefinition)
            {
                if (convention.TryGetOperationHandler(
                    completionContext,
                    configuration,
                    sortEnumValueDefinition,
                    out var handler))
                {
                    sortEnumValueDefinition.Handler = handler;
                }
                else
                {
                    throw SortInterceptor_NoOperationHandlerFoundForValue(
                        configuration,
                        sortEnumValueDefinition);
                }
            }
        }
    }

    private ISortConvention GetConvention(IDescriptorContext context, string? scope)
    {
        if (!_conventions.TryGetValue(scope ?? string.Empty, out var convention))
        {
            convention = context.GetSortConvention(scope);
            _conventions[scope ?? string.Empty] = convention;
        }

        return convention;
    }
}
