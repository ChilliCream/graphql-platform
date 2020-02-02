using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterOperationDescriptor
        : BooleanFilterOperationDescriptorBase
        , IBooleanFilterOperationDescriptor
    {
        private readonly BooleanFilterFieldDescriptor _descriptor;

        protected BooleanFilterOperationDescriptor(
            IDescriptorContext context,
            BooleanFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation)
           : base(context, name, type, operation)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor And() => _descriptor;

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptor Directive<T>()
           where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <summary>
        /// Create a new boolean filter operation descriptor.
        /// </summary>
        /// <param name="context">
        /// The descriptor context.
        /// </param>
        /// <param name="descriptor">
        /// The field descriptor on which this
        /// filter operation shall be applied.
        /// </param>
        /// <param name="name">
        /// The default name of the filter operation field.
        /// </param>
        /// <param name="type">
        /// The field type of this filter operation field.
        /// </param>
        /// <param name="operation">
        /// The filter operation info.
        /// </param>
        public static BooleanFilterOperationDescriptor New(
            IDescriptorContext context,
            BooleanFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new BooleanFilterOperationDescriptor(
                context, descriptor, name, type, operation);

    }
}
