using System;

namespace HotChocolate.Data.Sorting;

internal class SortConventionDescriptorProxy : ISortConventionDescriptor
{
    private readonly ISortConventionDescriptor _descriptor;

    public SortConventionDescriptorProxy(ISortConventionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public ISortOperationConventionDescriptor Operation(int operationId)
        => _descriptor.Operation(operationId);

    public ISortConventionDescriptor DefaultBinding<TSortType>()
        => _descriptor.DefaultBinding<TSortType>();

    public ISortConventionDescriptor BindRuntimeType<TRuntimeType, TSortType>()
        => _descriptor.BindRuntimeType<TRuntimeType, TSortType>();

    public ISortConventionDescriptor BindRuntimeType(Type runtimeType, Type sortType)
        => _descriptor.BindRuntimeType(runtimeType, sortType);

    public ISortConventionDescriptor ConfigureEnum<TSortEnumType>(ConfigureSortEnumType configure)
        where TSortEnumType : SortEnumType
        => _descriptor.ConfigureEnum<TSortEnumType>(configure);

    public ISortConventionDescriptor Configure<TSortType>(ConfigureSortInputType configure)
        where TSortType : SortInputType
        => _descriptor.Configure<TSortType>(configure);

    public ISortConventionDescriptor Configure<TSortType, TRuntimeType>(
        ConfigureSortInputType<TRuntimeType> configure)
        where TSortType : SortInputType<TRuntimeType>
        => _descriptor.Configure<TSortType, TRuntimeType>(configure);

    public ISortConventionDescriptor Provider<TProvider>() where TProvider : class, ISortProvider
        => _descriptor.Provider<TProvider>();

    public ISortConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, ISortProvider
        => _descriptor.Provider(provider);

    public ISortConventionDescriptor Provider(Type provider)
        => _descriptor.Provider(provider);

    public ISortConventionDescriptor ArgumentName(NameString argumentName)
        => _descriptor.ArgumentName(argumentName);

    public ISortConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, ISortProviderExtension
        => _descriptor.AddProviderExtension<TExtension>();

    public ISortConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, ISortProviderExtension
        => _descriptor.AddProviderExtension(provider);
}
