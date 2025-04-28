using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Filters;

public class FilterConventionDescriptor : IFilterConventionDescriptor
{
    private readonly Dictionary<int, FilterOperationConventionDescriptor> _operations = new();

    protected FilterConventionDescriptor(IDescriptorContext context, string? scope)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Configuration.Scope = scope;
    }

    protected IDescriptorContext Context { get; }

    protected FilterConventionConfiguration Configuration { get; } = new();

    public FilterConventionConfiguration CreateConfiguration()
    {
        // collect all operation configurations and add them to the convention definition.
        foreach (var operation in _operations.Values)
        {
            Configuration.Operations.Add(operation.CreateDefinition());
        }

        return Configuration;
    }

    /// <inheritdoc />
    public IFilterOperationConventionDescriptor Operation(int operationId)
    {
        if (_operations.TryGetValue(
                operationId,
                out var descriptor))
        {
            return descriptor;
        }

        descriptor = FilterOperationConventionDescriptor.New(operationId);
        _operations.Add(operationId, descriptor);

        return descriptor;
    }

    /// <inheritdoc />
    public IFilterConventionDescriptor BindRuntimeType<TRuntimeType, TFilterType>()
        where TFilterType : FilterInputType =>
        BindRuntimeType(typeof(TRuntimeType), typeof(TFilterType));

    /// <inheritdoc />
    public IFilterConventionDescriptor BindRuntimeType(Type runtimeType, Type filterType)
    {
        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        if (filterType is null)
        {
            throw new ArgumentNullException(nameof(filterType));
        }

        if (!typeof(FilterInputType).IsAssignableFrom(filterType))
        {
            throw new ArgumentException(
                FilterConventionDescriptor_MustInheritFromFilterInputType,
                nameof(filterType));
        }

        Configuration.Bindings[runtimeType] = filterType;

        return this;
    }

    /// <inheritdoc />
    public IFilterConventionDescriptor Configure<TFilterType>(ConfigureFilterInputType configure)
        where TFilterType : FilterInputType =>
        Configure(
            Context.TypeInspector.GetTypeRef(
                typeof(TFilterType),
                TypeContext.Input,
                Configuration.Scope),
            configure);

    /// <inheritdoc />
    public IFilterConventionDescriptor Configure<TFilterType, TRuntimeType>(
        ConfigureFilterInputType<TRuntimeType> configure)
        where TFilterType : FilterInputType<TRuntimeType> =>
        Configure(
            Context.TypeInspector.GetTypeRef(
                typeof(TFilterType),
                TypeContext.Input,
                Configuration.Scope),
            d =>
            {
                configure.Invoke(
                    FilterInputTypeDescriptor.From<TRuntimeType>(
                        (FilterInputTypeDescriptor)d,
                        Configuration.Scope));
            });

    protected IFilterConventionDescriptor Configure(
        TypeReference typeReference,
        ConfigureFilterInputType configure)
    {
        if (!Configuration.Configurations.TryGetValue(
                typeReference,
                out var configurations))
        {
            configurations = [];
            Configuration.Configurations.Add(typeReference, configurations);
        }

        configurations.Add(configure);

        return this;
    }

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider<TProvider>()
        where TProvider : class, IFilterProvider =>
        Provider(typeof(TProvider));

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IFilterProvider
    {
        Configuration.Provider = typeof(TProvider);
        Configuration.ProviderInstance = provider;

        return this;
    }

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider(Type provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (!typeof(IFilterProvider).IsAssignableFrom(provider))
        {
            throw new ArgumentException(
                FilterConventionDescriptor_MustImplementIFilterProvider,
                nameof(provider));
        }

        Configuration.Provider = provider;

        return this;
    }

    /// <inheritdoc />
    public IFilterConventionDescriptor ArgumentName(string argumentName)
    {
        Configuration.ArgumentName = argumentName;

        return this;
    }

    public IFilterConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, IFilterProviderExtension
    {
        Configuration.ProviderExtensionsTypes.Add(typeof(TExtension));

        return this;
    }

    public IFilterConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, IFilterProviderExtension
    {
        Configuration.ProviderExtensions.Add(provider);

        return this;
    }

    public IFilterConventionDescriptor AllowOr(bool allow = true)
    {
        Configuration.UseOr = allow;

        return this;
    }

    public IFilterConventionDescriptor AllowAnd(bool allow = true)
    {
        Configuration.UseAnd = allow;

        return this;
    }

    /// <summary>
    /// Creates a new descriptor for <see cref="FilterConvention"/>
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="scope">The scope</param>
    public static FilterConventionDescriptor New(IDescriptorContext context, string? scope) =>
        new FilterConventionDescriptor(context, scope);
}
