
using System;
using System.Reflection;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace Filtering.Customization
{
    public class DateTimeFilterFieldDescriptor
        : FilterFieldDescriptorBase,
        IDateTimeFilterFieldDescriptor
    {
        public DateTimeFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(FilterKind.DateTime, context, property, filterConventions)
        {
        }

        /// <inheritdoc/>
        public new IDateTimeFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IDateTimeFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IDateTimeFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IDateTimeFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        // We override this method for implicity binding
        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
                CreateOperation(operationKind).CreateDefinition();

        // The following to methods are for adding the filters explicitly
        public IDateTimeFilterOperationDescriptor AllowFrom() =>
            GetOrCreateOperation(FilterOperationKind.GreaterThanOrEquals);

        public IDateTimeFilterOperationDescriptor AllowTo() =>
            GetOrCreateOperation(FilterOperationKind.LowerThanOrEquals);

        // This is just a little helper that reduces code duplication
        private DateTimeFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind) =>
                Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));

        /// <inheritdoc/>
        public IDateTimeFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
                CreateOperation(operationKind).CreateDefinition();

        private DateTimeFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(
                operationKind, () => CreateOperation(operationKind));
        }

        private DateTimeFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(DateTime),
                Definition.Kind,
                operationKind,
                Definition.Property);

            return DateTimeFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation,
                FilterConvention);
        }
    }
}