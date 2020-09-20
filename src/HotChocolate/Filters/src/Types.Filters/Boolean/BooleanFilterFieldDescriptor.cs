using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IBooleanFilterFieldDescriptor
    {
        public BooleanFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals,
                FilterOperationKind.NotEquals,
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IBooleanFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public IBooleanFilterOperationDescriptor AllowEquals()
        {
            BooleanFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IBooleanFilterOperationDescriptor AllowNotEquals()
        {
            BooleanFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();


        private BooleanFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        private BooleanFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(bool),
                operationKind,
                Definition.Property);

            return BooleanFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }
    }
}
