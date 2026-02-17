using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting;

public class SortConventionDescriptor : ISortConventionDescriptor
{
    private readonly Dictionary<int, SortOperationConventionDescriptor> _operations = [];

    protected SortConventionDescriptor(IDescriptorContext context, string? scope)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Configuration.Scope = scope;
    }

    protected IDescriptorContext Context { get; }

    protected SortConventionConfiguration Configuration { get; } = new();

    public SortConventionConfiguration CreateConfiguration()
    {
        // collect all operation configurations and add them to the convention configuration.
        foreach (var operation in _operations.Values)
        {
            Configuration.Operations.Add(operation.CreateConfiguration());
        }

        return Configuration;
    }

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Operation(int operationId)
    {
        if (_operations.TryGetValue(
            operationId,
            out var descriptor))
        {
            return descriptor;
        }

        descriptor = SortOperationConventionDescriptor.New(operationId);
        _operations.Add(operationId, descriptor);
        return descriptor;
    }

    /// <inheritdoc />
    public ISortConventionDescriptor DefaultBinding<TSortType>()
    {
        Configuration.DefaultBinding = typeof(TSortType);
        return this;
    }

    /// <inheritdoc />
    public ISortConventionDescriptor BindRuntimeType<TRuntimeType, TSortType>() =>
        BindRuntimeType(typeof(TRuntimeType), typeof(TSortType));

    /// <inheritdoc />
    public ISortConventionDescriptor BindRuntimeType(Type runtimeType, Type sortType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        ArgumentNullException.ThrowIfNull(sortType);

        if (!typeof(SortInputType).IsAssignableFrom(sortType)
            && !typeof(SortEnumType).IsAssignableFrom(sortType))
        {
            throw new ArgumentException(
                SortConventionDescriptor_MustInheritFromSortInputOrEnumType,
                nameof(sortType));
        }

        Configuration.Bindings[runtimeType] = sortType;
        return this;
    }

    /// <inheritdoc />
    public ISortConventionDescriptor Configure<TSortType>(
        ConfigureSortInputType configure)
        where TSortType : SortInputType =>
        Configure(
            Context.TypeInspector.GetTypeRef(
                typeof(TSortType),
                TypeContext.Input,
                Configuration.Scope),
            configure);

    /// <inheritdoc />
    public ISortConventionDescriptor Configure<TSortType, TRuntimeType>(
        ConfigureSortInputType<TRuntimeType> configure)
        where TSortType : SortInputType<TRuntimeType> =>
        Configure(
            Context.TypeInspector.GetTypeRef(
                typeof(TSortType),
                TypeContext.Input,
                Configuration.Scope),
            d =>
            {
                configure.Invoke(
                    SortInputTypeDescriptor.From<TRuntimeType>(
                        (SortInputTypeDescriptor)d,
                        Configuration.Scope));
            });

    /// <inheritdoc />
    public ISortConventionDescriptor ConfigureEnum<TSortEnumType>(
        ConfigureSortEnumType configure)
        where TSortEnumType : SortEnumType
    {
        var typeReference =
            Context.TypeInspector.GetTypeRef(
                typeof(TSortEnumType),
                TypeContext.None,
                Configuration.Scope);

        if (!Configuration.EnumConfigurations.TryGetValue(
            typeReference,
            out var configurations))
        {
            configurations = [];
            Configuration.EnumConfigurations.Add(typeReference, configurations);
        }

        configurations.Add(configure);
        return this;
    }

    protected ISortConventionDescriptor Configure(
        TypeReference typeReference,
        ConfigureSortInputType configure)
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
    public ISortConventionDescriptor Provider<TProvider>()
        where TProvider : class, ISortProvider =>
        Provider(typeof(TProvider));

    /// <inheritdoc />
    public ISortConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, ISortProvider
    {
        Configuration.Provider = typeof(TProvider);
        Configuration.ProviderInstance = provider;
        return this;
    }

    /// <inheritdoc />
    public ISortConventionDescriptor Provider(Type provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!typeof(ISortProvider).IsAssignableFrom(provider))
        {
            throw new ArgumentException(
                SortConventionDescriptor_MustImplementISortProvider,
                nameof(provider));
        }

        Configuration.Provider = provider;
        return this;
    }

    /// <inheritdoc />
    public ISortConventionDescriptor ArgumentName(string argumentName)
    {
        Configuration.ArgumentName = argumentName;
        return this;
    }

    public ISortConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, ISortProviderExtension
    {
        Configuration.ProviderExtensionsTypes.Add(typeof(TExtension));
        return this;
    }

    public ISortConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, ISortProviderExtension
    {
        Configuration.ProviderExtensions.Add(provider);
        return this;
    }

    /// <summary>
    /// Creates a new descriptor for <see cref="SortConvention"/>
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="scope">The scope</param>
    public static SortConventionDescriptor New(IDescriptorContext context, string? scope)
        => new(context, scope);
}
