using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor<TObject>
        : ObjectFilterFieldDescriptor
        , IObjectFilterFieldDescriptor<TObject>
    {
        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConvention)
            : base(context, property, typeof(TObject), filterConvention)
        {
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor<TObject> Name(NameString value)
        {
            base.Name(value);
            return this;
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
                Definition.Kind,
                operationKind,
                Definition.Property);

            return ObjectFilterOperationDescriptor<TObject>.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation,
                FilterConvention);
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
