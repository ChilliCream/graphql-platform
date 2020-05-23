using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IArrayFilterOperationDescriptor
    {
        private readonly ArrayFilterFieldDescriptor _descriptor;

        protected ArrayFilterOperationDescriptor(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context, name, type, operation, filterConventions)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public IArrayFilterFieldDescriptor And() => _descriptor;

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor Directive<T>(
            T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterOperationDescriptor Directive(
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
        /// <param name="filterConventions">
        /// The filter conventions
        /// </param>
        public static ArrayFilterOperationDescriptor New(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConvention) =>
            new ArrayFilterOperationDescriptor(
                context, descriptor, name, type, operation, filterConvention);
    }
}
