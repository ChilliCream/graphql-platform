using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
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
            Type type)
            : base(context, property)
        {
            _type = type;
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.ArraySome,
                FilterOperationKind.ArrayNone,
                FilterOperationKind.ArrayAll,
                FilterOperationKind.ArrayAny
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
                operationKind,
                Definition.Property);
        }

        protected ExtendedTypeReference GetTypeReference() =>
            Context.TypeInspector.GetTypeRef(
                typeof(FilterInputType<>).MakeGenericType(_type),
                Definition.Type.Context);

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
            var operation = GetFilterOperation(operationKind);
            var typeReference = GetTypeReference();
            return ArrayFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation);
        }

        private ArrayBooleanFilterOperationDescriptor CreateBooleanOperation(
          FilterOperationKind operationKind)
        {
            var operation = GetFilterOperation(operationKind);

            var typeReference = RewriteTypeToNullableType(
                Context.TypeInspector.GetTypeRef(typeof(bool)),
                Context.TypeInspector);

            return ArrayBooleanFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation);
        }
    }
}
