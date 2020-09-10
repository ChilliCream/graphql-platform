using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Extensions;

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

        private ObjectFilterOperationDescriptor<TObject> GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        public IObjectFilterOperationDescriptor<TObject> AllowObject(
            Action<IFilterInputTypeDescriptor<TObject>> descriptor)
        {
            ObjectFilterOperationDescriptor<TObject> field =
                GetOrCreateOperation(FilterOperationKind.Object);
            var type = new FilterInputType<TObject>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            field.Type(typeReference);
            return field;
        }

        public IObjectFilterOperationDescriptor<TObject> AllowObject<TFilter>()
            where TFilter : FilterInputType<TObject>
        {
            ObjectFilterOperationDescriptor<TObject> field =
                GetOrCreateOperation(FilterOperationKind.Object);
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
