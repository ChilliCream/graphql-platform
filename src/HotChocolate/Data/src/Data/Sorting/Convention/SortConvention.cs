using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// The sort convention provides defaults for inferring sorting fields.
/// </summary>
public class SortConvention
    : Convention<SortConventionDefinition>
    , ISortConvention
{
    private const string _typePostFix = "SortInput";

    private Action<ISortConventionDescriptor>? _configure;
    private INamingConventions _namingConventions = default!;
    private IReadOnlyDictionary<int, SortOperation> _operations = default!;
    private IDictionary<Type, Type> _bindings = default!;

    private IDictionary<TypeReference, List<ConfigureSortInputType>> _inputTypeConfigs =
        default!;

    private IDictionary<TypeReference, List<ConfigureSortEnumType>> _enumTypeConfigs =
        default!;

    private string _argumentName = default!;
    private ISortProvider _provider = default!;
    private ITypeInspector _typeInspector = default!;
    private Type? _defaultBinding;

    protected SortConvention()
    {
        _configure = Configure;
    }

    public SortConvention(Action<ISortConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    internal new SortConventionDefinition? Definition => base.Definition;

    protected override SortConventionDefinition CreateDefinition(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(SortConvention_NoConfigurationSpecified);
        }

        var descriptor = SortConventionDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(ISortConventionDescriptor descriptor)
    {
    }

    protected internal override void Complete(IConventionContext context)
    {
        if (Definition?.Provider is null)
        {
            throw SortConvention_NoProviderFound(GetType(), Definition?.Scope);
        }

        if (Definition.ProviderInstance is null)
        {
            _provider =
                context.Services.GetOrCreateService<ISortProvider>(Definition.Provider) ??
                throw SortConvention_NoProviderFound(GetType(), Definition.Scope);
        }
        else
        {
            _provider = Definition.ProviderInstance;
        }

        _namingConventions = context.DescriptorContext.Naming;

        _operations = Definition.Operations.ToDictionary(
            x => x.Id,
            SortOperation.FromDefinition);

        foreach (var operation in _operations.Values)
        {
            if (string.IsNullOrEmpty(operation.Name))
            {
                throw SortConvention_OperationIsNotNamed(this, operation);
            }
        }

        _bindings = Definition.Bindings;
        _defaultBinding = Definition.DefaultBinding;
        _inputTypeConfigs = Definition.Configurations;
        _enumTypeConfigs = Definition.EnumConfigurations;
        _argumentName = Definition.ArgumentName;

        if (_provider is ISortProviderConvention init)
        {
            var extensions =
                CollectExtensions(context.Services, Definition);
            init.Initialize(context);
            MergeExtensions(context, init, extensions);
            init.Complete(context);
        }

        _typeInspector = context.DescriptorContext.TypeInspector;

        // It is important to always call base to continue the cleanup and the disposal of the
        // definition
        base.Complete(context);
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type runtimeType) =>
        _namingConventions.GetTypeName(runtimeType, TypeKind.Object) + _typePostFix;

    /// <inheritdoc />
    public virtual string? GetTypeDescription(Type runtimeType) =>
        _namingConventions.GetTypeDescription(runtimeType, TypeKind.InputObject);

    /// <inheritdoc />
    public virtual string GetFieldName(MemberInfo member) =>
        _namingConventions.GetMemberName(member, MemberKind.InputObjectField);

    /// <inheritdoc />
    public virtual string? GetFieldDescription(MemberInfo member) =>
        _namingConventions.GetMemberDescription(member, MemberKind.InputObjectField);

    /// <inheritdoc />
    public virtual ExtendedTypeReference GetFieldType(MemberInfo member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (TryCreateSortType(
            _typeInspector.GetReturnType(member, true),
            out var returnType))
        {
            return _typeInspector.GetTypeRef(returnType, TypeContext.Input, Scope);
        }

        throw SortConvention_TypeOfMemberIsUnknown(member);
    }

    /// <inheritdoc />
    public string GetOperationName(int operation)
    {
        if (_operations.TryGetValue(operation, out var operationConvention))
        {
            return operationConvention.Name;
        }

        throw SortConvention_OperationNameNotFound(operation);
    }

    /// <inheritdoc />
    public string? GetOperationDescription(int operationId)
    {
        if (_operations.TryGetValue(operationId, out var operationConvention))
        {
            return operationConvention.Description;
        }

        return null;
    }

    /// <inheritdoc />
    public string GetArgumentName() => _argumentName;

    /// <inheritdoc cref="ISortConvention"/>
    public void ApplyConfigurations(
        TypeReference typeReference,
        ISortInputTypeDescriptor descriptor)
    {
        if (_inputTypeConfigs.TryGetValue(
            typeReference,
            out var configurations))
        {
            foreach (var configure in configurations)
            {
                configure(descriptor);
            }

            if (descriptor is SortInputTypeDescriptor inputTypeDescriptor)
            {
                inputTypeDescriptor.CreateDefinition();
            }
        }
    }

    public void ApplyConfigurations(
        TypeReference typeReference,
        ISortEnumTypeDescriptor descriptor)
    {
        if (_enumTypeConfigs.TryGetValue(
            typeReference,
            out var configurations))
        {
            foreach (var configure in configurations)
            {
                configure(descriptor);
            }
        }
    }

    public IQueryBuilder CreateBuilder<TEntityType>() =>
        _provider.CreateBuilder<TEntityType>(_argumentName);

    public virtual void ConfigureField(IObjectFieldDescriptor descriptor) =>
        _provider.ConfigureField(_argumentName, descriptor);

    public bool TryGetOperationHandler(
        ITypeCompletionContext context,
        EnumTypeDefinition typeDefinition,
        SortEnumValueDefinition fieldDefinition,
        [NotNullWhen(true)] out ISortOperationHandler? handler)
    {
        foreach (var sortFieldHandler in _provider.OperationHandlers)
        {
            if (sortFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
            {
                handler = sortFieldHandler;
                return true;
            }
        }

        handler = null;
        return false;
    }

    public bool TryGetFieldHandler(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition,
        [NotNullWhen(true)] out ISortFieldHandler? handler)
    {
        foreach (var sortFieldHandler in _provider.FieldHandlers)
        {
            if (sortFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
            {
                handler = sortFieldHandler;
                return true;
            }
        }

        handler = null;
        return false;
    }

    public ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition)
        => _provider.CreateMetaData(context, typeDefinition, fieldDefinition);

    private bool TryCreateSortType(
        IExtendedType runtimeType,
        [NotNullWhen(true)] out Type? type)
    {
        if (_bindings.TryGetValue(runtimeType.Source, out type))
        {
            return true;
        }

        if (runtimeType.IsArrayOrList)
        {
            return false;
        }

        if (runtimeType.Type.IsClass ||
            runtimeType.Type.IsInterface)
        {
            type = typeof(SortInputType<>).MakeGenericType(runtimeType.Source);
            return true;
        }

        if (_defaultBinding is { })
        {
            type = _defaultBinding;
            return true;
        }

        type = null;
        return false;
    }

    private static IReadOnlyList<ISortProviderExtension> CollectExtensions(
        IServiceProvider serviceProvider,
        SortConventionDefinition definition)
    {
        var extensions = new List<ISortProviderExtension>();
        extensions.AddRange(definition.ProviderExtensions);
        foreach (var extensionType in definition.ProviderExtensionsTypes)
        {
            if (serviceProvider.TryGetOrCreateService<ISortProviderExtension>(
                extensionType,
                out var createdExtension))
            {
                extensions.Add(createdExtension);
            }
        }

        return extensions;
    }

    private static void MergeExtensions(
        IConventionContext context,
        ISortProviderConvention provider,
        IReadOnlyList<ISortProviderExtension> extensions)
    {
        if (provider is not Convention providerConvention)
        {
            return;
        }

        for (var m = 0; m < extensions.Count; m++)
        {
            if (extensions[m] is ISortProviderConvention extensionConvention)
            {
                extensionConvention.Initialize(context);
                extensions[m].Merge(context, providerConvention);
                extensionConvention.Complete(context);
            }
        }
    }
}
