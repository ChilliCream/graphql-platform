using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// The sort convention provides defaults for inferring sorting fields.
/// </summary>
public class SortConvention
    : Convention<SortConventionConfiguration>
        , ISortConvention
{
    private const string InputPostFix = "SortInput";
    private const string InputTypePostFix = "SortInputType";

    private Action<ISortConventionDescriptor>? _configure;
    private INamingConventions _namingConventions = null!;
    private IReadOnlyDictionary<int, SortOperation> _operations = null!;
    private IDictionary<Type, Type> _bindings = null!;
    private IDictionary<TypeReference, List<ConfigureSortInputType>> _inputTypeConfigs = null!;
    private IDictionary<TypeReference, List<ConfigureSortEnumType>> _enumTypeConfigs = null!;
    private string _argumentName = null!;
    private ISortProvider _provider = null!;
    private ITypeInspector _typeInspector = null!;
    private Type? _defaultBinding;

    protected SortConvention()
    {
        _configure = Configure;
    }

    public SortConvention(Action<ISortConventionDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    internal new SortConventionConfiguration? Configuration => base.Configuration;

    protected override SortConventionConfiguration CreateConfiguration(
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

        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(ISortConventionDescriptor descriptor)
    {
    }

    protected internal override void Complete(IConventionContext context)
    {
        if (Configuration?.Provider is null)
        {
            throw SortConvention_NoProviderFound(GetType(), Configuration?.Scope);
        }

        if (Configuration.ProviderInstance is null)
        {
            _provider =
                (ISortProvider)GetServiceOrCreateInstance(context.Services, Configuration.Provider)
                ?? throw SortConvention_NoProviderFound(GetType(), Configuration.Scope);
        }
        else
        {
            _provider = Configuration.ProviderInstance;
        }

        _namingConventions = context.DescriptorContext.Naming;

        _operations = Configuration.Operations.ToDictionary(
            x => x.Id,
            SortOperation.FromConfiguration);

        foreach (var operation in _operations.Values)
        {
            if (string.IsNullOrEmpty(operation.Name))
            {
                throw SortConvention_OperationIsNotNamed(this, operation);
            }
        }

        _bindings = Configuration.Bindings;
        _defaultBinding = Configuration.DefaultBinding;
        _inputTypeConfigs = Configuration.Configurations;
        _enumTypeConfigs = Configuration.EnumConfigurations;
        _argumentName = Configuration.ArgumentName;

        if (_provider is ISortProviderConvention init)
        {
            var extensions = CollectExtensions(context.Services, Configuration);
            init.Initialize(context, this);
            MergeExtensions(context, init, extensions);
            init.Complete(context);
        }

        _typeInspector = context.DescriptorContext.TypeInspector;

        // It is important to always call base to continue the cleanup and the disposal of the
        // configuration
        base.Complete(context);
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type runtimeType)
    {
        var name = _namingConventions.GetTypeName(runtimeType);

        var isInputObjectType = typeof(SortInputType).IsAssignableFrom(runtimeType);
        var isEndingInput = name.EndsWith(InputPostFix, StringComparison.Ordinal);
        var isEndingInputType = name.EndsWith(InputTypePostFix, StringComparison.Ordinal);

        if (isInputObjectType && isEndingInputType)
        {
            return name[..^4];
        }

        if (isInputObjectType && !isEndingInput && !isEndingInputType)
        {
            return name + InputPostFix;
        }

        if (!isInputObjectType && !isEndingInput)
        {
            return name + InputPostFix;
        }

        return name;
    }

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
        ArgumentNullException.ThrowIfNull(member);

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
                inputTypeDescriptor.CreateConfiguration();
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
        EnumTypeConfiguration typeDefinition,
        SortEnumValueConfiguration fieldConfiguration,
        [NotNullWhen(true)] out ISortOperationHandler? handler)
    {
        foreach (var sortFieldHandler in _provider.OperationHandlers)
        {
            if (sortFieldHandler.CanHandle(context, typeDefinition, fieldConfiguration))
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
        ISortInputTypeConfiguration typeConfiguration,
        ISortFieldConfiguration fieldConfiguration,
        [NotNullWhen(true)] out ISortFieldHandler? handler)
    {
        foreach (var sortFieldHandler in _provider.FieldHandlers)
        {
            if (sortFieldHandler.CanHandle(context, typeConfiguration, fieldConfiguration))
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
        ISortInputTypeConfiguration typeConfiguration,
        ISortFieldConfiguration fieldConfiguration)
        => _provider.CreateMetaData(context, typeConfiguration, fieldConfiguration);

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

        if (runtimeType.Type.IsClass || runtimeType.Type.IsInterface)
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
        SortConventionConfiguration configuration)
    {
        var extensions = new List<ISortProviderExtension>();
        extensions.AddRange(configuration.ProviderExtensions);
        foreach (var extensionType in configuration.ProviderExtensionsTypes)
        {
            extensions.Add((ISortProviderExtension)GetServiceOrCreateInstance(serviceProvider, extensionType));
        }

        return extensions;
    }

    private void MergeExtensions(
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
                extensionConvention.Initialize(context, this);
                extensions[m].Merge(context, providerConvention);
                extensionConvention.Complete(context);
            }
        }
    }
}
