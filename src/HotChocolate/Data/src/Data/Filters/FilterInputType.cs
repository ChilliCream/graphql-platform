using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputType
        : InputObjectType
    {
        private readonly Action<IFilterInputTypeDescriptor> _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(Action<IFilterInputTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = FilterInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                context.Scope,
                GetType());

            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(
            IFilterInputTypeDescriptor descriptor)
        {
        }

        protected override void OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            fields.Add(new AndField(context.DescriptorContext, this));
            fields.Add(new OrField(context.DescriptorContext, this));

            foreach (InputFieldDefinition fieldDefinition in definition.Fields)
            {
                if (fieldDefinition is FilterOperationFieldDefinition operation)
                {
                    fields.Add(new FilterOperationField(operation));
                }
                else if (fieldDefinition is FilterFieldDefinition field)
                {
                    fields.Add(new FilterField(field));
                }
            }
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            SetTypeIdentity(typeof(FilterInputType<>));
        }

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}