using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortInputType<T> : SortInputType
{
    private Action<ISortInputTypeDescriptor<T>>? _configure;

    public SortInputType(Action<ISortInputTypeDescriptor<T>> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    [ActivatorUtilitiesConstructor]
    public SortInputType()
    {
        _configure = Configure;
    }

    protected override InputObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = SortInputTypeDescriptor.New<T>(
            context.DescriptorContext,
            typeof(T),
            context.Scope);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(ISortInputTypeDescriptor<T> descriptor)
    {
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(
        ISortInputTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}
