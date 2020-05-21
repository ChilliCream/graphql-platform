using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputType
        : InputObjectType
        , IFilterInputType
    {
        private readonly Action<IFilterInputTypeDescriptor> _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(
            Action<IFilterInputTypeDescriptor> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public Type EntityType { get; private set; } = typeof(object);

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = FilterInputTypeDescriptor.New(
                context.DescriptorContext,
                context.DescriptorContext.GetFilterConvention());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            SetTypeIdentity(typeof(FilterInputType<>));
        }

        protected virtual void Configure(
            IFilterInputTypeDescriptor descriptor)
        {
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (definition is FilterInputTypeDefinition ft &&
                ft.EntityType is { })
            {
                EntityType = ft.EntityType;
            }
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

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}
