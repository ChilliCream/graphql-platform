
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
        private static readonly ClrTypeReference _areaTypeReference =
            new ClrTypeReference(typeof(AreaFilterType), TypeContext.Input, true, true);

        private static readonly ClrTypeReference _distanceTypeReference =
            new ClrTypeReference(typeof(DistanceFilterType), TypeContext.Input, true, true);

        private static readonly ClrTypeReference _intersectsTypeReference =
            new ClrTypeReference(typeof(IntersectsFilterType), TypeContext.Input, true, true);

        private static readonly ClrTypeReference _lengthTypeReference =
            new ClrTypeReference(typeof(LengthFilterType), TypeContext.Input, true, true);

        private static readonly ClrTypeReference _withinTypeReference =
            new ClrTypeReference(typeof(WithinFilterType), TypeContext.Input, true, true);

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

        // We override this method for implicit binding
        protected override FilterOperationDefintion CreateOperationDefinition(
            int operationKind) =>
                CreateOperation(operationKind).CreateDefinition();

        // The following to methods are for adding the filters explicitly
        public IGeometryFilterOperationDescriptor AllowArea() =>
            Filters.GetOrAddOperation(
                SpatialFilterOperation.Area,
                CreateArea);

        public IGeometryFilterOperationDescriptor AllowCrosses() =>
            throw new NotImplementedException();

        public IGeometryFilterOperationDescriptor AllowDistance() =>
            Filters.GetOrAddOperation(
                SpatialFilterOperation.Distance,
                CreateDistance);

        public IGeometryFilterOperationDescriptor AllowIntersects() =>
             Filters.GetOrAddOperation(
                SpatialFilterOperation.Intersects,
                CreateIntersects);

        public IGeometryFilterOperationDescriptor AllowLength() =>
            throw new NotImplementedException();

        public IGeometryFilterOperationDescriptor AllowWithin() =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public IGeometryFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        private GeometryFilterOperationDescriptor CreateArea()
        {
            var operation = new FilterOperation(
                            typeof(Geometry),
                            Definition.Kind,
                            SpatialFilterOperation.Area,
                            Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(SpatialFilterOperation.Area),
                _areaTypeReference,
                operation,
                FilterConvention);
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

        private GeometryFilterOperationDescriptor CreateIntersects()
        {
            var operation = new FilterOperation(
                            typeof(Geometry),
                            Definition.Kind,
                            SpatialFilterOperation.Intersects,
                            Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(SpatialFilterOperation.Intersects),
                _intersectsTypeReference,
                operation,
                FilterConvention);
        }

        private GeometryFilterOperationDescriptor CreateLength()
        {
            var operation = new FilterOperation(
                            typeof(Geometry),
                            Definition.Kind,
                            SpatialFilterOperation.Length,
                            Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(SpatialFilterOperation.Length),
                _lengthTypeReference,
                operation,
                FilterConvention);
        }

        private GeometryFilterOperationDescriptor CreateWithin()
        {
            var operation = new FilterOperation(
                            typeof(Geometry),
                            Definition.Kind,
                            SpatialFilterOperation.Within,
                            Definition.Property);

            return GeometryFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(SpatialFilterOperation.Within),
                _withinTypeReference,
                operation,
                FilterConvention);
        }

        private GeometryFilterOperationDescriptor CreateOperation(
            int operationKind)
        {
            switch (operationKind) {
                case SpatialFilterOperation.Area:
                    return CreateArea();
                case SpatialFilterOperation.Distance:
                    return CreateDistance();
                case SpatialFilterOperation.Intersects:
                    return CreateIntersects();
                case SpatialFilterOperation.Length:
                    return CreateLength();
                case SpatialFilterOperation.Within:
                    return CreateWithin();
                default: {
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
    }
}
