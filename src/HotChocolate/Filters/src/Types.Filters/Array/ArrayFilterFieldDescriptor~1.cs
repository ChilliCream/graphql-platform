using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterFieldDescriptor<TArray>
        : ArrayFilterFieldDescriptor
        , IArrayFilterFieldDescriptor<TArray>
    {
        public ArrayFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(context, property, typeof(TArray), filterConventions)
        {
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor<TArray> Name(NameString value)
        {
            base.Name(value);
            return this;
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
                GetOrCreateOperation(FilterOperationKind.ArraySome);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowSome<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                GetOrCreateOperation(FilterOperationKind.ArraySome);
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
                GetOrCreateOperation(FilterOperationKind.ArrayNone);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowNone<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                GetOrCreateOperation(FilterOperationKind.ArrayNone);
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
                GetOrCreateOperation(FilterOperationKind.ArrayAll);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IArrayFilterOperationDescriptor<TArray> AllowAll<TFilter>()
            where TFilter : FilterInputType<TArray>
        {
            ArrayFilterOperationDescriptor<TArray> field =
                GetOrCreateOperation(FilterOperationKind.ArrayAll);
            field.Type<TFilter>();
            Filters.Add(field);

            return field;
        }

        public new IArrayFilterOperationDescriptor<TArray> AllowAll()
        {
            return AllowAll<FilterInputType<TArray>>();
        }

        private ArrayFilterOperationDescriptor<TArray> GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        private ArrayFilterOperationDescriptor<TArray> CreateOperation(
            FilterOperationKind operationKind)
        {
            FilterOperation? operation = GetFilterOperation(operationKind);
            ClrTypeReference? typeReference = GetTypeReference();
            return ArrayFilterOperationDescriptor<TArray>.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeReference,
                operation,
                FilterConvention);
        }
    }
}
