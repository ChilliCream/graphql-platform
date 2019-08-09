using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ComparableFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IComparableFilterFieldDescriptor
    {
        public ComparableFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals,
                FilterOperationKind.NotEquals,

                FilterOperationKind.In,
                FilterOperationKind.NotIn,

                FilterOperationKind.GreaterThan,
                FilterOperationKind.NotGreaterThan,

                FilterOperationKind.GreaterThanOrEquals,
                FilterOperationKind.NotGreaterThanOrEquals,

                FilterOperationKind.LowerThan,
                FilterOperationKind.NotLowerThan,

                FilterOperationKind.LowerThanOrEquals,
                FilterOperationKind.NotLowerThanOrEquals
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowEquals()
        {
            ComparableFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotEquals()
        {
            ComparableFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowIn()
        {
            ComparableFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotIn()
        {
            ComparableFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.NotIn);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowGreaterThan()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.GreaterThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotGreaterThan()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.NotGreaterThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowGreaterThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.GreaterThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotGreaterThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.NotGreaterThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowLowerThan()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.LowerThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotLowerThan()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.NotLowerThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowLowerThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.LowerThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotLowerThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 CreateOperation(FilterOperationKind.NotLowerThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor Ignore()
        {
            Definition.Ignore = true;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private ComparableFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(IComparable),
                operationKind,
                Definition.Property);

            return ComparableFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }
    }
}
