using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor
       : FilterFieldDescriptorBase
       , IObjectFilterFieldDescriptor
    {
        private readonly Type type;
        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type)
            : base(context, property)
        {
            this.type = type;
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

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
                type,
                operationKind,
                Definition.Property);

            var opetationDescirptor = ObjectFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
            ;
            opetationDescirptor.Type(new ClrTypeReference(typeof(FilterInputType<>).MakeGenericType(type), Definition.Type.Context, true, true));
            return opetationDescirptor;
        }

    }

    public class ObjectFilterFieldDescriptor<TObject>
        : ObjectFilterFieldDescriptor
        , IObjectFilterFieldDescriptor<TObject>
    {
        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property, typeof(TObject))
        {

            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

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

        public IObjectFilterFieldDescriptor<TObject> AllowObject(Action<IFilterInputTypeDescriptor<TObject>> descriptor)
        {
            var type = new FilterInputType<TObject>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Equals);
            field.Type(typeReference);
            Filters.Add(field);
            return this;
        }

        public IObjectFilterFieldDescriptor<TObject> AllowObject<TFilter>() where TFilter : FilterInputType<TObject>
        {
            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Equals);
            field.Type<TFilter>();
            Filters.Add(field);

            return this;
        }
    }
}
