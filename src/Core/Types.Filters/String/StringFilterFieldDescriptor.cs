using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

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
                Context,
                this,
                Definition.Name,
                RewriteTypeToNullableType(),
                operation);

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
                Context,
                this,
                // TODO : conventions _contains
                Definition.Name + "_contains",
                RewriteTypeToNullableListType(),
                operation);

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
                Context,
                this,
                // TODO : conventions _in
                Definition.Name + "_in",
                RewriteTypeToNullableListType(),
                operation);

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
                Context,
                this,
                // TODO : conventions _starts_with
                Definition.Name + "_starts_with",
                RewriteTypeToNullableType(),
                operation);

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
                Context,
                this,
                // TODO : conventions _ends_with
                Definition.Name + "_ends_with",
                RewriteTypeToNullableType(),
                operation);

            Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }
    }
}
