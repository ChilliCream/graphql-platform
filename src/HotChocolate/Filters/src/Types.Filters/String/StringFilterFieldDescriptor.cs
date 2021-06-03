using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class StringFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IStringFilterFieldDescriptor
    {
        public StringFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals,
                FilterOperationKind.NotEquals,

                FilterOperationKind.Contains,
                FilterOperationKind.NotContains,

                FilterOperationKind.StartsWith,
                FilterOperationKind.NotStartsWith,

                FilterOperationKind.EndsWith,
                FilterOperationKind.NotEndsWith,

                FilterOperationKind.In,
                FilterOperationKind.NotIn
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowEquals()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotEquals()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowContains()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Contains);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotContains()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotContains);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowIn()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotIn()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.StartsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotStartsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotStartsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.EndsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotEndsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEndsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private StringFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                () => CreateOperation(operationKind));
        }

        private StringFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(string),
                operationKind,
                Definition.Property);

            return StringFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }
    }
}
