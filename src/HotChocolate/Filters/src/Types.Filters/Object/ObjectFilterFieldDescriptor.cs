using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

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
            Type type,
            IFilterConvention filterConventions)
            : base(FilterKind.Object, context, property, filterConventions)
        {
            _type = type;
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

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
                Definition.Kind,
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
                operation,
                FilterConvention);
        }

        private ObjectFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                () => CreateOperation(operationKind));
        }

        public IObjectFilterOperationDescriptor AllowObject()
        {
            ObjectFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Object);
            Filters.Add(field);
            return field;
        }
    }
}
