using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputType<T>
        : InputObjectType
        , IFilterInputType
    {
        private readonly Action<IFilterInputObjectTypeDescriptor<T>> _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(
            Action<IFilterInputObjectTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        #region Configuration

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = FilterInputObjectTypeDescriptor<T>.New(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(
            IFilterInputObjectTypeDescriptor<T> descriptor)
        {

        }

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
