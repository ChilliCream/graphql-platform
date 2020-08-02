using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ArrayBooleanFilterOperationDescriptor 
        : BooleanFilterOperationDescriptorBase
        , IArrayBooleanFilterOperationDescriptor
    {
        private readonly ArrayFilterFieldDescriptor _descriptor;

        protected ArrayBooleanFilterOperationDescriptor(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) : base(context, name, type, operation)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public IArrayFilterFieldDescriptor And()
        {
            return _descriptor;
        }

        /// <inheritdoc/>
        public new IArrayBooleanFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayBooleanFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayBooleanFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayBooleanFilterOperationDescriptor Directive<T>()
           where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IArrayBooleanFilterOperationDescriptor Directive(
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
        public static ArrayBooleanFilterOperationDescriptor New(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new ArrayBooleanFilterOperationDescriptor(
                context, descriptor, name, type, operation);
    }
}
