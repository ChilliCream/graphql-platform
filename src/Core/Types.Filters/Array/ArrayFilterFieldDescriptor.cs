using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IArrayFilterFieldDescriptor
    {
        private readonly Type _type;

        public ArrayFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type)
            : base(context, property)
        {
            _type = type;
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.ArraySome
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IArrayFilterFieldDescriptor BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IArrayFilterFieldDescriptor BindImplicitly() =>
            BindFilters(BindingBehavior.Implicit);


        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private ArrayFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                _type,
                operationKind,
                Definition.Property);

            return ArrayFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                new ClrTypeReference(
                    typeof(FilterInputType<>).MakeGenericType(_type),
                    Definition.Type.Context,
                    true,
                    true),
                operation);
            ;
        }


        public IArrayFilterOperationDescriptor AllowSome()
        {
            ArrayFilterOperationDescriptor field =
                CreateOperation(FilterOperationKind.ArraySome);
            Filters.Add(field);
            return field;
        }

    }
}
