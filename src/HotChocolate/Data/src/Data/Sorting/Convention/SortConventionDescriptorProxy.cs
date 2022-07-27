using System;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// A proxy object that wraps <see cref="ISortConventionDescriptor"/>
/// </summary>
public class SortConventionDescriptorProxy : ISortConventionDescriptor
{
    private readonly ISortConventionDescriptor _descriptor;

    /// <summary>
    /// Creates a new proxy object
    /// </summary>
    public SortConventionDescriptorProxy(ISortConventionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Operation(int operationId)
        => _descriptor.Operation(operationId);

    /// <inheritdoc />
    public ISortConventionDescriptor DefaultBinding<TSortType>()
        => _descriptor.DefaultBinding<TSortType>();

    /// <inheritdoc />
    public ISortConventionDescriptor BindRuntimeType<TRuntimeType, TSortType>()
        => _descriptor.BindRuntimeType<TRuntimeType, TSortType>();

    /// <inheritdoc />
    public ISortConventionDescriptor BindRuntimeType(Type runtimeType, Type sortType)
        => _descriptor.BindRuntimeType(runtimeType, sortType);

    /// <inheritdoc />
    public ISortConventionDescriptor ConfigureEnum<TSortEnumType>(ConfigureSortEnumType configure)
        where TSortEnumType : SortEnumType
        => _descriptor.ConfigureEnum<TSortEnumType>(configure);

    /// <inheritdoc />
    public ISortConventionDescriptor Configure<TSortType>(ConfigureSortInputType configure)
        where TSortType : SortInputType
        => _descriptor.Configure<TSortType>(configure);

    /// <inheritdoc />
    public ISortConventionDescriptor Configure<TSortType, TRuntimeType>(
        ConfigureSortInputType<TRuntimeType> configure)
        where TSortType : SortInputType<TRuntimeType>
        => _descriptor.Configure<TSortType, TRuntimeType>(configure);

    /// <inheritdoc />
    public ISortConventionDescriptor Provider<TProvider>() where TProvider : class, ISortProvider
        => _descriptor.Provider<TProvider>();

    /// <inheritdoc />
    public ISortConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, ISortProvider
        => _descriptor.Provider(provider);

    /// <inheritdoc />
    public ISortConventionDescriptor Provider(Type provider)
        => _descriptor.Provider(provider);

    /// <inheritdoc />
    public ISortConventionDescriptor ArgumentName(string argumentName)
        => _descriptor.ArgumentName(argumentName);

    /// <inheritdoc />
    public ISortConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, ISortProviderExtension
        => _descriptor.AddProviderExtension<TExtension>();

    /// <inheritdoc />
    public ISortConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, ISortProviderExtension
        => _descriptor.AddProviderExtension(provider);
}
