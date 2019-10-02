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

            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.ArraySome
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

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


        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private ArrayFilterOperationDescriptor<TArray> CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(TArray),
                operationKind,
                Definition.Property);

            return ArrayFilterOperationDescriptor<TArray>.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }

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
    }
}
