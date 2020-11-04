using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputType
        : InputObjectType
        , IFilterInputType
    {
        private Action<IFilterInputTypeDescriptor>? _configure;

        public FilterInputType()
        {
            _configure = Configure;
        }

        public FilterInputType(Action<IFilterInputTypeDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public IExtendedType EntityType { get; private set; } = default!;

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = FilterInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType(),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            if (definition is FilterInputTypeDefinition { EntityType: { } } filterDefinition)
            {
                SetTypeIdentity(typeof(FilterInputType<>)
                    .MakeGenericType(filterDefinition.EntityType));
            }
        }

        protected virtual void Configure(IFilterInputTypeDescriptor descriptor)
        {
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (definition is FilterInputTypeDefinition ft &&
                ft.EntityType is { })
            {
                EntityType = context.TypeInspector.GetType(ft.EntityType);
            }
        }

        protected override void OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            if (definition is FilterInputTypeDefinition { UseAnd: true } def)
            {
                fields.Add(new AndField(context.DescriptorContext, def.Scope));
            }

            if (definition is FilterInputTypeDefinition { UseOr: true } defOr)
            {
                fields.Add(new OrField(context.DescriptorContext, defOr.Scope));
            }

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

        // we are disabling the default configure method so
        // that this does not lead to confusion.
        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}
