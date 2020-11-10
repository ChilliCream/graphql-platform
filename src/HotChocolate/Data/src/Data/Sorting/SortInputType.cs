using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortInputType
        : InputObjectType
        , ISortInputType
    {
        private Action<ISortInputTypeDescriptor>? _configure;

        public SortInputType()
        {
            _configure = Configure;
        }

        public SortInputType(Action<ISortInputTypeDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public IExtendedType EntityType { get; private set; } = default!;

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = SortInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType(),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(ISortInputTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            if (definition is SortInputTypeDefinition { EntityType: { } } sortDefinition)
            {
                SetTypeIdentity(
                    typeof(SortInputType<>).MakeGenericType(sortDefinition.EntityType));
            }
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (definition is SortInputTypeDefinition ft &&
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
            foreach (InputFieldDefinition fieldDefinition in definition.Fields)
            {
                if (fieldDefinition is SortFieldDefinition field)
                {
                    fields.Add(new SortField(field));
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
