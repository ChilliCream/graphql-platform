
using System;
using System.Reflection;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public class GeometryFilterFieldDescriptor
        : FilterFieldDescriptorBase,
        IGeometryFilterFieldDescriptor
    {
        private static readonly ClrTypeReference _distanceTypeReference =
            new ClrTypeReference(typeof(DistanceFilterType), TypeContext.Input, true, true);

        public GeometryFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(SpatialFilterKind.Geometry, context, property, filterConventions)
        {
        }

        /// <inheritdoc/>
        public new IGeometryFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IGeometryFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IGeometryFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IGeometryFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        // We override this method for implicity binding
        protected override FilterOperationDefintion CreateOperationDefinition(
            int operationKind) =>
                CreateOperation(operationKind).CreateDefinition();

        // The following to methods are for adding the filters explicitly
        public IGeometryFilterOperationDescriptor AllowDistance() =>
            Filters.GetOrAddOperation(
                SpatialFilterOperation.Distance,
                CreateDistance);

        /// <inheritdoc/>
        public IGeometryFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        private GeometryFilterOperationDescriptor CreateDistance()
        {
            var operation = new FilterOperation(
                            typeof(Geometry),
                            SpatialFilterOperation.Distance,
                            SpatialFilterOperation.Distance,
                            Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(SpatialFilterOperation.Distance),
                _distanceTypeReference,
                operation,
                FilterConvention);
        }

        private GeometryFilterOperationDescriptor CreateOperation(
            int operationKind)
        {
            if (SpatialFilterOperation.Distance.Equals(operationKind))
            {
                return CreateDistance();
            }

            var operation = new FilterOperation(
                typeof(Geometry),
                Definition.Kind,
                operationKind,
                Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation,
                FilterConvention);
        }
    }
}