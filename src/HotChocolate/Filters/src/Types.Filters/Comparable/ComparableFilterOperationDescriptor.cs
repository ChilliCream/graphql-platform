using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class ComparableFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IComparableFilterOperationDescriptor
    {
        private readonly ComparableFilterFieldDescriptor _descriptor;

        protected ComparableFilterOperationDescriptor(
            IDescriptorContext context,
            ComparableFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context, name, type, operation, filterConventions)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor And() => _descriptor;

        /// <inheritdoc/>
        public new IComparableFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        /// <summary>
        /// Create a new comparable filter operation descriptor.
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
        public static ComparableFilterOperationDescriptor New(
            IDescriptorContext context,
            ComparableFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation,
            IFilterConvention filterConventions) =>
            new ComparableFilterOperationDescriptor(
                context, descriptor, name, type, operation, filterConventions);
    }
}
