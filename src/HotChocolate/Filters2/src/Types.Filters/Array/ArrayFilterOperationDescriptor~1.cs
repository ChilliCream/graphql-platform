using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{

    public class ArrayFilterOperationDescriptor<TArray>
        : ArrayFilterOperationDescriptor
        , IArrayFilterOperationDescriptor<TArray>
    {
        private readonly ArrayFilterFieldDescriptor<TArray> _descriptor;

        protected ArrayFilterOperationDescriptor(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor<TArray> descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation)
            : base(context, descriptor, name, type, operation)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor<TArray> And() => _descriptor;

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor<TArray> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor<TArray> Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor<TArray> Directive<T>(
            T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor<TArray> Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor<TArray> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <summary>
        /// Create a new string filter operation descriptor.
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
        public static ArrayFilterOperationDescriptor<TArray> New(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor<TArray> descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new ArrayFilterOperationDescriptor<TArray>(
                context, descriptor, name, type, operation);
    }
}
