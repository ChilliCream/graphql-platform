using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IObjectFilterFieldDescriptor
    {
        private readonly Type _type;

        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type)
            : base(context, property)
        {
            _type = type;
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Object
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IObjectFilterFieldDescriptor BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IObjectFilterFieldDescriptor BindImplicitly() =>
            BindFilters(BindingBehavior.Implicit);


        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private ObjectFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                _type,
                operationKind,
                Definition.Property);

            return ObjectFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                new ClrTypeReference(
                    typeof(FilterInputType<>).MakeGenericType(_type),
                    Definition.Type.Context,
                    true,
                    true),
                operation);
        }


        public IObjectFilterOperationDescriptor AllowObject()
        {
            ObjectFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.Object);
            Filters.Add(field);
            return field;
        }

    }
}
