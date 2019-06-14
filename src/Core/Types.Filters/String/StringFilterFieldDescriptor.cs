using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    // TODO : we also need not equals
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
                FilterOperationKind.Contains,
                FilterOperationKind.StartsWith,
                FilterOperationKind.EndsWith,
                FilterOperationKind.In
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        public IStringFilterOperationDescriptor AllowEquals()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowContains()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.Contains);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowIn()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.StartsWith);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.EndsWith);
            Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        public IStringFilterFieldDescriptor BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        public IStringFilterFieldDescriptor BindImplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

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
