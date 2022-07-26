using System;

namespace HotChocolate.Data.Filters;

internal class FilterConventionDescriptorProxy : IFilterConventionDescriptor
{
    private readonly IFilterConventionDescriptor _descriptor;

    public FilterConventionDescriptorProxy(IFilterConventionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public IFilterOperationConventionDescriptor Operation(int operationId)
        => _descriptor.Operation(operationId);

    public IFilterConventionDescriptor BindRuntimeType<TRuntimeType, TFilterType>()
        where TFilterType : FilterInputType
        => _descriptor.BindRuntimeType<TRuntimeType, TFilterType>();

    public IFilterConventionDescriptor BindRuntimeType(Type runtimeType, Type filterType)
        => _descriptor.BindRuntimeType(runtimeType, filterType);

    public IFilterConventionDescriptor Configure<TFilterType>(ConfigureFilterInputType configure)
        where TFilterType : FilterInputType
        => _descriptor.Configure<TFilterType>(configure);

    public IFilterConventionDescriptor Configure<TFilterType, TRuntimeType>(
        ConfigureFilterInputType<TRuntimeType> configure)
        where TFilterType : FilterInputType<TRuntimeType>
        => _descriptor.Configure<TFilterType, TRuntimeType>(configure);

    public IFilterConventionDescriptor Provider<TProvider>()
        where TProvider : class, IFilterProvider
        => _descriptor.Provider<TProvider>();

    public IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IFilterProvider
        => _descriptor.Provider(provider);

    public IFilterConventionDescriptor Provider(Type provider)
        => _descriptor.Provider(provider);

    public IFilterConventionDescriptor ArgumentName(NameString argumentName)
        => _descriptor.ArgumentName(argumentName);

    public IFilterConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, IFilterProviderExtension
        => _descriptor.AddProviderExtension<TExtension>();

    public IFilterConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, IFilterProviderExtension
        => _descriptor.AddProviderExtension(provider);

    public IFilterConventionDescriptor AllowOr(bool allow = true)
        => _descriptor.AllowOr(allow);

    public IFilterConventionDescriptor AllowAnd(bool allow = true)
        => _descriptor.AllowAnd(allow);
}
