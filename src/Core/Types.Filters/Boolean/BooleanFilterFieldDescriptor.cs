using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    // TODO : we also need not equals
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

        public IBooleanFilterOperationDescriptor AllowEquals()
        {
            BooleanFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }
        public IBooleanFilterOperationDescriptor AllowNotEquals()
        {
            BooleanFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        public new IBooleanFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        public IBooleanFilterFieldDescriptor BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        public IBooleanFilterFieldDescriptor BindImplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private BooleanFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(string),
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
