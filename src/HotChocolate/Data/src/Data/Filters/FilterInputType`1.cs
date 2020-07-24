using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputType<T>
        : FilterInputType
    {
        private readonly Action<IFilterInputTypeDescriptor<T>> _configure;

        protected FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(Action<IFilterInputTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = FilterInputTypeDescriptor<T>.New(
                context.DescriptorContext,
                context.Scope,
                typeof(T));

            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            SetTypeIdentity(typeof(FilterInputType<T>));
        }

        protected virtual void Configure(
            IFilterInputTypeDescriptor<T> descriptor)
        {
        }

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}