using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor<TObject>
        : ObjectFilterFieldDescriptor
        , IObjectFilterFieldDescriptor<TObject>
    {
        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property, typeof(TObject))
        {
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor<TObject> BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor<TObject> BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor<TObject> BindImplicitly() =>
            BindFilters(BindingBehavior.Implicit);


        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private ObjectFilterOperationDescriptor<TObject> CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(TObject),
                operationKind,
                Definition.Property);

            return ObjectFilterOperationDescriptor<TObject>.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }

        public IObjectFilterOperationDescriptor<TObject> AllowObject(
            Action<IFilterInputTypeDescriptor<TObject>> descriptor)
        {
            var type = new FilterInputType<TObject>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Object);
            field.Type(typeReference);
            Filters.Add(field);
            return field;
        }

        public IObjectFilterOperationDescriptor<TObject> AllowObject<TFilter>()
            where TFilter : FilterInputType<TObject>
        {
            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Object);
            field.Type<TFilter>();
            Filters.Add(field);

            return field;
        }

        public new IObjectFilterOperationDescriptor<TObject> AllowObject()
        {
            return AllowObject<FilterInputType<TObject>>();
        }
    }
}
