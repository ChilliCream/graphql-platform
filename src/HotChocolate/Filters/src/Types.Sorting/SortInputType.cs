using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortInputType<T>
        : InputObjectType
        , ISortInputType
    {
        private readonly Action<ISortInputTypeDescriptor<T>> _configure;

        public SortInputType()
        {
            _configure = Configure;
        }

        public SortInputType(
            Action<ISortInputTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public Type EntityType { get; private set; } = typeof(object);

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            SortInputTypeDescriptor<T> descriptor =
                SortInputTypeDescriptor<T>.New(
                    context.DescriptorContext,
                    typeof(T));
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            SetTypeIdentity(typeof(SortInputType<>));
        }

        protected virtual void Configure(
            ISortInputTypeDescriptor<T> descriptor)
        {
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            if (definition is SortInputTypeDefinition ft &&
                ft.EntityType is { })
            {
                EntityType = ft.EntityType;
            }
        }

        protected override void OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            foreach (SortOperationDefintion fieldDefinition in
                definition.Fields.OfType<SortOperationDefintion>())
            {
                fields.Add(new SortOperationField(fieldDefinition));
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
