using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputType<T>
        : FilterInputType
    {
        private Action<IFilterInputTypeDescriptor<T>>? _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(Action<IFilterInputTypeDescriptor<T>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = FilterInputTypeDescriptor.New<T>(
                context.DescriptorContext,
                typeof(T),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IFilterInputTypeDescriptor<T> descriptor)
        {
        }

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IFilterInputTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}
