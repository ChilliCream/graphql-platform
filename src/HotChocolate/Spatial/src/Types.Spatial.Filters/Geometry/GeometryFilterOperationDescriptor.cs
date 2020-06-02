using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Spatial.Filters
{
    public class GeometryFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IGeometryFilterOperationDescriptor
    {
        private readonly IGeometryFilterFieldDescriptor _descriptor;

        protected GeometryFilterOperationDescriptor(
            IDescriptorContext context,
            IGeometryFilterFieldDescriptor descriptor,
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
        public IGeometryFilterFieldDescriptor And() => _descriptor;

        /// <inheritdoc/>
        public new IGeometryFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IGeometryFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IGeometryFilterOperationDescriptor Directive<T>(
            T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IGeometryFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IGeometryFilterOperationDescriptor Directive(
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
        public static GeometryFilterOperationDescriptor New(
            IDescriptorContext context,
            IGeometryFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions) =>
            new GeometryFilterOperationDescriptor(
                context, descriptor, name, type, operation, filterConventions);
    }
}