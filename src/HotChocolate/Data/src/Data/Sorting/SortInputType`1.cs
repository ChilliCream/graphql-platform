using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortInputType<T>
        : SortInputType
    {
        private Action<ISortInputTypeDescriptor<T>>? _configure;

        public SortInputType()
        {
            _configure = Configure;
        }

        public SortInputType(Action<ISortInputTypeDescriptor<T>> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
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
}
