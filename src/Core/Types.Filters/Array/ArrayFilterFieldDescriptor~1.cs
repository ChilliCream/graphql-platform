using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterFieldDescriptor<TArray>
        : ArrayFilterFieldDescriptor
        , IArrayFilterFieldDescriptor<TArray>
    {
        public ArrayFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property, typeof(TArray))
        {
        }


        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor<TArray> BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor<TArray> BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor<TArray> BindImplicitly() =>
            BindFilters(BindingBehavior.Implicit);


        public IArrayFilterOperationDescriptor<TArray> AllowSome(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor)
        {
            var type = new FilterInputType<TArray>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArraySome);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowSome<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArraySome);
            field.Type<TFilter>();
            Filters.Add(field);

            return field;
        }

        public new IArrayFilterOperationDescriptor<TArray> AllowSome()
        {
            return AllowSome<FilterInputType<TArray>>();
        }

        public IArrayFilterOperationDescriptor<TArray> AllowNone(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor)
        {
            var type = new FilterInputType<TArray>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArrayNone);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowNone<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArrayNone);
            field.Type<TFilter>();
            Filters.Add(field);

            return field;
        }

        public new IArrayFilterOperationDescriptor<TArray> AllowNone()
        {
            return AllowNone<FilterInputType<TArray>>();
        }

        public IArrayFilterOperationDescriptor<TArray> AllowAll(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor)
        {
            var type = new FilterInputType<TArray>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArrayAll);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowAll<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                CreateOperation(FilterOperationKind.ArrayAll);
            field.Type<TFilter>();
            Filters.Add(field);

            return field;
        }

        public new IArrayFilterOperationDescriptor<TArray> AllowAll()
        {
            return AllowAll<FilterInputType<TArray>>();
        }

        private ArrayFilterOperationDescriptor<TArray> CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = GetFilterOperation(operationKind);
            var typeReference = GetTypeReference();
            return ArrayFilterOperationDescriptor<TArray>.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation);
        }
    }
}
