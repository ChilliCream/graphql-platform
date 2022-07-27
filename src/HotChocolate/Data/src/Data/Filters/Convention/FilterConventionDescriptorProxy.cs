using System;

namespace HotChocolate.Data.Filters;

/// <summary>
/// A proxy object that wraps <see cref="IFilterConventionDescriptor"/>
/// </summary>
public class FilterConventionDescriptorProxy : IFilterConventionDescriptor
{
    private readonly IFilterConventionDescriptor _descriptor;

    /// <summary>
    /// Creates a new proxy object
    /// </summary>
    public FilterConventionDescriptorProxy(IFilterConventionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    /// <inheritdoc />
    public IFilterOperationConventionDescriptor Operation(int operationId)
        => _descriptor.Operation(operationId);

    /// <inheritdoc />
    public IFilterConventionDescriptor BindRuntimeType<TRuntimeType, TFilterType>()
        where TFilterType : FilterInputType
        => _descriptor.BindRuntimeType<TRuntimeType, TFilterType>();

    /// <inheritdoc />
    public IFilterConventionDescriptor BindRuntimeType(Type runtimeType, Type filterType)
        => _descriptor.BindRuntimeType(runtimeType, filterType);

    /// <inheritdoc />
    public IFilterConventionDescriptor Configure<TFilterType>(ConfigureFilterInputType configure)
        where TFilterType : FilterInputType
        => _descriptor.Configure<TFilterType>(configure);

    /// <inheritdoc />
    public IFilterConventionDescriptor Configure<TFilterType, TRuntimeType>(
        ConfigureFilterInputType<TRuntimeType> configure)
        where TFilterType : FilterInputType<TRuntimeType>
        => _descriptor.Configure<TFilterType, TRuntimeType>(configure);

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider<TProvider>()
        where TProvider : class, IFilterProvider
        => _descriptor.Provider<TProvider>();

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IFilterProvider
        => _descriptor.Provider(provider);

    /// <inheritdoc />
    public IFilterConventionDescriptor Provider(Type provider)
        => _descriptor.Provider(provider);

    /// <inheritdoc />
    public IFilterConventionDescriptor ArgumentName(string argumentName)
        => _descriptor.ArgumentName(argumentName);

    /// <inheritdoc />
    public IFilterConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, IFilterProviderExtension
        => _descriptor.AddProviderExtension<TExtension>();

    /// <inheritdoc />
    public IFilterConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, IFilterProviderExtension
        => _descriptor.AddProviderExtension(provider);

    /// <inheritdoc />
    public IFilterConventionDescriptor AllowOr(bool allow = true)
        => _descriptor.AllowOr(allow);

    /// <inheritdoc />
    public IFilterConventionDescriptor AllowAnd(bool allow = true)
        => _descriptor.AllowAnd(allow);
}
