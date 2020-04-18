using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

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
            Type type,
            IFilterConvention filtersConventions)
            : base(FilterKind.Array, context, property, filtersConventions)
        {
            _type = type;
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

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

        public IArrayFilterOperationDescriptor AllowSome()
        {
            ArrayFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.ArraySome);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor AllowNone()
        {
            ArrayFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.ArrayNone);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor AllowAll()
        {
            ArrayFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.ArrayAll);
            Filters.Add(field);
            return field;
        }

        public IArrayBooleanFilterOperationDescriptor AllowAny()
        {
            ArrayBooleanFilterOperationDescriptor field =
                GetOrCreateBooleanOperation(FilterOperationKind.ArrayAny);
            Filters.Add(field);
            return field;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind)
        {
            return FilterOperationKind.ArrayAny == operationKind
                ? CreateBooleanOperation(operationKind).CreateDefinition()
                : CreateOperation(operationKind).CreateDefinition();
        }

        protected FilterOperation GetFilterOperation(
            FilterOperationKind operationKind)
        {
            return new FilterOperation(
                _type,
                Definition.Kind,
                operationKind,
                Definition.Property);
        }

        protected ClrTypeReference GetTypeReference()
        {
            return new ClrTypeReference(
                typeof(FilterInputType<>).MakeGenericType(_type),
                    Definition.Type.Context,
                    true,
                    true);
        }

        private ArrayFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        private ArrayBooleanFilterOperationDescriptor GetOrCreateBooleanOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateBooleanOperation(operationKind));
        }

        private ArrayFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            FilterOperation? operation = GetFilterOperation(operationKind);
            ClrTypeReference? typeReference = GetTypeReference();
            return ArrayFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation,
                FilterConvention);
        }

        private ArrayBooleanFilterOperationDescriptor CreateBooleanOperation(
          FilterOperationKind operationKind)
        {
            FilterOperation? operation = GetFilterOperation(operationKind);

            ITypeReference? typeReference = RewriteTypeToNullableType(
                new ClrTypeReference(typeof(bool),
                    Definition.Type.Context,
                    true,
                    true));

            return ArrayBooleanFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation,
                FilterConvention);
        }
    }
}
