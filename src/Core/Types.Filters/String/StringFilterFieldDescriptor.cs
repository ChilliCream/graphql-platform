using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.String
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
        }

        public IStringFilterOperationDescriptor AllowEquals()
        {
            var operation = new FilterOperation(
                typeof(string),
                FilterOperationKind.Equals,
                Definition.Property);

            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name, Definition.Type, operation);

            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowContains()
        {
            var operation = new FilterOperation(
                typeof(string),
                FilterOperationKind.Equals,
                Definition.Property);

            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name + "_contains", Definition.Type, operation);

            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowIn()
        {
            var operation = new FilterOperation(
                typeof(string),
                FilterOperationKind.Equals,
                Definition.Property);

            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name + "_in", Definition.Type, operation);

            Filters.Add(field);
            return field;
        }
        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            var operation = new FilterOperation(
                typeof(string),
                FilterOperationKind.Equals,
                Definition.Property);

            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name + "_starts_with", Definition.Type, operation);

            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            var operation = new FilterOperation(
                typeof(string),
                FilterOperationKind.Equals,
                Definition.Property);

            var field = StringFilterOperationDescriptor.New(
                Context, this, Definition.Name + "_ends_with", Definition.Type, operation);

            Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

    }
}
