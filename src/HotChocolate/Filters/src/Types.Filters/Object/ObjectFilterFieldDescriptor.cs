using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IObjectFilterFieldDescriptor
    {
        private readonly Type _type = typeof(object);

        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type,
            IFilterConvention filterConventions)
            : base(FilterKind.Object, context, property, filterConventions)
        {
            _type = type;
            SetTypeReference(_type);
        }

        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            IFilterConvention filterConventions)
            : base(FilterKind.Object, context, filterConventions)
        {
        }

        /// <inheritdoc/>
        public new IObjectFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

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

        public new IObjectFilterFieldDescriptor Type<TInputType>()
            where TInputType : FilterInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IObjectFilterFieldDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : FilterInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IObjectFilterFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IObjectFilterFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            int operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        protected void SetTypeReference(Type type)
        {
            Type(typeof(FilterInputType<>).MakeGenericType(type));
        }

        private ObjectFilterOperationDescriptor CreateOperation(
            int operationKind)
        {
            var operation = new FilterOperation(
                _type,
                Definition.Kind,
                operationKind,
                Definition.Property);

            return ObjectFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                Definition.Type,
                operation,
                FilterConvention);
        }

        private ObjectFilterOperationDescriptor GetOrCreateOperation(
            int operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                () => CreateOperation(operationKind));
        }

        public IObjectFilterOperationDescriptor AllowObject()
        {
            ObjectFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Object);
            Filters.Add(field);
            return field;
        }

        public static ObjectFilterFieldDescriptor New(
            IDescriptorContext context,
            IFilterConvention filterConventions,
            NameString name)
        {
            var descriptor = new ObjectFilterFieldDescriptor(
                context, filterConventions);

            descriptor.Name(name);
            return descriptor;
        }
    }
}
