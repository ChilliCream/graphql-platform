using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputType<T>
        : InputObjectType
        , IFilterInputType
    {
        private readonly Action<IFilterInputTypeDescriptor<T>> _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(
            Action<IFilterInputTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        #region Configuration

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = FilterInputTypeDescriptor<T>.New(
                context.DescriptorContext,
                typeof(T));
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(
            IFilterInputTypeDescriptor<T> descriptor)
        {
        }

        protected override void OnCompleteFields(
            ICompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            fields.Add(new AndField(context.DescriptorContext, this));
            fields.Add(new OrField(context.DescriptorContext, this));

            foreach (FilterOperationDefintion fieldDefinition in
                definition.Fields.OfType<FilterOperationDefintion>())
            {
                fields.Add(new FilterOperationField(fieldDefinition));
            }
        }

        #endregion

        #region Disabled

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
