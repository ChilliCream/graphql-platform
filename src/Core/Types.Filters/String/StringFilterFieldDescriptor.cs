using System;
using System.Collections.Generic;
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

        protected override void OnCompleteFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds)
        {
            if (Definition.Filters.BindingBehavior == BindingBehavior.Implicit)
            {
                AddImplicitOperation(
                    fields,
                    handledFilterKinds,
                    FilterOperationKind.Equals);

                AddImplicitOperation(
                    fields,
                    handledFilterKinds,
                    FilterOperationKind.Contains);

                AddImplicitOperation(
                    fields,
                    handledFilterKinds,
                    FilterOperationKind.In);

                AddImplicitOperation(
                    fields,
                    handledFilterKinds,
                    FilterOperationKind.StartsWith);

                AddImplicitOperation(
                    fields,
                    handledFilterKinds,
                    FilterOperationKind.EndsWith);
            }
        }

        private void AddImplicitOperation(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<FilterOperationKind> handledFilterKinds,
            FilterOperationKind operationKind)
        {
            if (handledFilterKinds.Add(operationKind))
            {
                FilterOperationDefintion definition =
                    CreateOperation(
                        RewriteTypeToNullableType(),
                        operationKind)
                        .CreateDefinition();

                if (!fields.ContainsKey(definition.Name))
                {
                    fields.Add(definition.Name, definition);
                }
            }
        }

        public IStringFilterOperationDescriptor AllowEquals()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(
                    RewriteTypeToNullableType(),
                    FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowContains()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(
                    RewriteTypeToNullableType(),
                    FilterOperationKind.Contains);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowIn()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(
                    RewriteTypeToNullableListType(),
                    FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(
                    RewriteTypeToNullableType(),
                    FilterOperationKind.StartsWith);
            Filters.Add(field);
            return field;
        }

        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            StringFilterOperationDescriptor field =
                CreateOperation(
                    RewriteTypeToNullableType(),
                    FilterOperationKind.EndsWith);
            Filters.Add(field);
            return field;
        }

        public new IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        private StringFilterOperationDescriptor CreateOperation(
            ITypeReference reference,
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
                reference,
                operation);
        }
    }
}
