using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.ThrowHelper;
using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace HotChocolate.Data.Filters;

/// <summary>
/// The filter convention provides defaults for inferring filters.
/// </summary>
public class FilterConvention
    : Convention<FilterConventionDefinition>
        , IFilterConvention
{
    private const string _inputPostFix = "FilterInput";
    private const string _inputTypePostFix = "FilterInputType";

    private Action<IFilterConventionDescriptor>? _configure;
    private INamingConventions _namingConventions = default!;
    private IReadOnlyDictionary<int, FilterOperation> _operations = default!;
    private IDictionary<Type, Type> _bindings = default!;
    private IDictionary<TypeReference, List<ConfigureFilterInputType>> _configs = default!;

    private string _argumentName = default!;
    private IFilterProvider _provider = default!;
    private ITypeInspector _typeInspector = default!;
    private bool _useAnd;
    private bool _useOr;

    protected FilterConvention()
    {
        _configure = Configure;
    }

    public FilterConvention(Action<IFilterConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    internal new FilterConventionDefinition? Definition => base.Definition;

    /// <inheritdoc />
    protected override FilterConventionDefinition CreateDefinition(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(
                DataResources.FilterConvention_NoConfigurationSpecified);
        }

        var descriptor = FilterConventionDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    /// <summary>
    /// This method is called on initialization of the convention but before the convention is
    /// completed. The default implementation of this method does nothing. It can be overridden
    /// by a derived class such that the convention can be further configured before it is
    /// completed
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor that can be used to configure the convention
    /// </param>
    protected virtual void Configure(IFilterConventionDescriptor descriptor) { }

    /// <inheritdoc />
    protected internal override void Complete(IConventionContext context)
    {
        if (Definition?.Provider is null)
        {
            throw FilterConvention_NoProviderFound(GetType(), Definition?.Scope);
        }

        if (Definition.ProviderInstance is null)
        {
            _provider =
                (IFilterProvider)GetServiceOrCreateInstance(context.Services, Definition.Provider) ??
                throw FilterConvention_NoProviderFound(GetType(), Definition.Scope);
        }
        else
        {
            _provider = Definition.ProviderInstance;
        }

        _namingConventions = context.DescriptorContext.Naming;
        _operations =
            Definition.Operations.ToDictionary(x => x.Id, FilterOperation.FromDefinition);
        _bindings = Definition.Bindings;
        _configs = Definition.Configurations;
        _argumentName = Definition.ArgumentName;
        _useAnd = Definition.UseAnd;
        _useOr = Definition.UseOr;

        if (_provider is IFilterProviderConvention init)
        {
            var extensions =
                CollectExtensions(context.Services, Definition);
            init.Initialize(context, this);
            MergeExtensions(context, init, extensions);
            init.Complete(context);
        }

        _typeInspector = context.DescriptorContext.TypeInspector;

        // It is important to always call base to continue the cleanup and the disposal of the
        // definition
        base.Complete(context);
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type runtimeType)
    {
        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        if (typeof(IEnumOperationFilterInputType).IsAssignableFrom(runtimeType) &&
            runtimeType.GenericTypeArguments.Length == 1 &&
            runtimeType.GetGenericTypeDefinition() == typeof(EnumOperationFilterInputType<>))
        {
            var genericName = _namingConventions.GetTypeName(runtimeType.GenericTypeArguments[0]);

            return genericName + "OperationFilterInput";
        }

        if (typeof(IComparableOperationFilterInputType).IsAssignableFrom(runtimeType) &&
            runtimeType.GenericTypeArguments.Length == 1 &&
            runtimeType.GetGenericTypeDefinition() ==
            typeof(ComparableOperationFilterInputType<>))
        {
            var genericName = _namingConventions.GetTypeName(runtimeType.GenericTypeArguments[0]);

            return $"Comparable{genericName}OperationFilterInput";
        }

        if (typeof(IListFilterInputType).IsAssignableFrom(runtimeType) &&
            runtimeType.GenericTypeArguments.Length == 1)
        {
            var genericType = runtimeType.GenericTypeArguments[0];

            var genericName = typeof(FilterInputType).IsAssignableFrom(genericType)
                ? GetTypeName(genericType)
                : _namingConventions.GetTypeName(genericType);

            return "List" + genericName;
        }

        var name = _namingConventions.GetTypeName(runtimeType);

        var isInputObjectType = typeof(FilterInputType).IsAssignableFrom(runtimeType);
        var isEndingInput = name.EndsWith(_inputPostFix, StringComparison.Ordinal);
        var isEndingInputType = name.EndsWith(_inputTypePostFix, StringComparison.Ordinal);

        if (isInputObjectType && isEndingInputType)
        {
            return name.Substring(0, name.Length - 4);
        }

        if (isInputObjectType && !isEndingInput && !isEndingInputType)
        {
            return name + _inputPostFix;
        }

        if (!isInputObjectType && !isEndingInput)
        {
            return name + _inputPostFix;
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
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (TryCreateFilterType(_typeInspector.GetReturnType(member, true), out var rt))
        {
            return _typeInspector.GetTypeRef(rt, TypeContext.Input, Scope);
        }

        throw FilterConvention_TypeOfMemberIsUnknown(member);
    }

    /// <inheritdoc />
    public string GetOperationName(int operation)
    {
        if (_operations.TryGetValue(operation, out var operationConvention))
        {
            return operationConvention.Name;
        }

        throw FilterConvention_OperationNameNotFound(operation);
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

    /// <inheritdoc cref="IFilterConvention"/>
    public void ApplyConfigurations(
        TypeReference typeReference,
        IFilterInputTypeDescriptor descriptor)
    {
        if (_configs.TryGetValue(
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

    public bool IsAndAllowed()
    {
        return _useAnd;
    }

    public bool IsOrAllowed()
    {
        return _useOr;
    }

    public bool TryGetHandler(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition,
        [NotNullWhen(true)] out IFilterFieldHandler? handler)
    {
        foreach (var filterFieldHandler in _provider.FieldHandlers)
        {
            if (filterFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
            {
                handler = filterFieldHandler;

                return true;
            }
        }

        handler = null;

        return false;
    }

    public IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => _provider.CreateMetaData(context, typeDefinition, fieldDefinition);

    protected bool TryCreateFilterType(
        IExtendedType runtimeType,
        [NotNullWhen(true)] out Type? type)
    {
        if (_bindings.TryGetValue(runtimeType.Source, out type))
        {
            return true;
        }

        if (runtimeType.IsArrayOrList)
        {
            if (runtimeType.ElementType is { } &&
                TryCreateFilterType(runtimeType.ElementType, out var elementType))
            {
                type = typeof(ListFilterInputType<>).MakeGenericType(elementType);

                return true;
            }
        }

        if (runtimeType.Type.IsEnum)
        {
            type = typeof(EnumOperationFilterInputType<>).MakeGenericType(runtimeType.Source);

            return true;
        }

        if (runtimeType.Type is { IsValueType: true, IsPrimitive: false, })
        {
            type = typeof(FilterInputType<>).MakeGenericType(runtimeType.Type);

            return true;
        }

        if (runtimeType.Type.IsClass || runtimeType.Type.IsInterface)
        {
            type = typeof(FilterInputType<>).MakeGenericType(runtimeType.Source);

            return true;
        }

        type = null;

        return false;
    }

    private static IReadOnlyList<IFilterProviderExtension> CollectExtensions(
        IServiceProvider serviceProvider,
        FilterConventionDefinition definition)
    {
        var extensions = new List<IFilterProviderExtension>();
        extensions.AddRange(definition.ProviderExtensions);

        foreach (var extensionType in definition.ProviderExtensionsTypes)
        {
            extensions.Add((IFilterProviderExtension)GetServiceOrCreateInstance(serviceProvider, extensionType));
        }

        return extensions;
    }

    private void MergeExtensions(
        IConventionContext context,
        IFilterProviderConvention provider,
        IReadOnlyList<IFilterProviderExtension> extensions)
    {
        if (provider is not Convention providerConvention)
        {
            return;
        }

        for (var m = 0; m < extensions.Count; m++)
        {
            if (extensions[m] is IFilterProviderConvention extensionConvention)
            {
                extensionConvention.Initialize(context, this);
                extensions[m].Merge(context, providerConvention);
                extensionConvention.Complete(context);
            }
        }
    }
}
