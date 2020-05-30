using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class ArrayFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IArrayFilterFieldDescriptor
    {
        private readonly Type? _type;

        public ArrayFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type,
            IFilterConvention filtersConventions)
            : base(FilterKind.Array, context, property, filtersConventions)
        {
            _type = type;
            SetTypeReference(_type);
        }

        public ArrayFilterFieldDescriptor(
            IDescriptorContext context,
            IFilterConvention filtersConventions)
            : base(FilterKind.Array, context, filtersConventions)
        {
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IArrayFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior)
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

        public new IArrayFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IArrayFilterFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IArrayFilterFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IArrayFilterFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(object operationKind)
        {
            return FilterOperationKind.ArrayAny.Equals(operationKind)
                ? CreateBooleanOperation(operationKind).CreateDefinition()
                : CreateOperation(operationKind).CreateDefinition();
        }

        protected FilterOperation GetFilterOperation(object operationKind)
        {
            return new FilterOperation(
                _type,
                Definition.Kind,
                operationKind,
                Definition.Property);
        }

        protected void SetTypeReference(Type type)
        {
            Type(typeof(FilterInputType<>).MakeGenericType(type));
        }

        private ArrayFilterOperationDescriptor GetOrCreateOperation(object operationKind)
        {
            return Filters.GetOrAddOperation(operationKind, () => CreateOperation(operationKind));
        }

        private ArrayBooleanFilterOperationDescriptor GetOrCreateBooleanOperation(
            object operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateBooleanOperation(operationKind));
        }

        private ArrayFilterOperationDescriptor CreateOperation(object operationKind)
        {
            FilterOperation? operation = GetFilterOperation(operationKind);
            return ArrayFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                Definition.Type,
                operation,
                FilterConvention);
        }

        private ArrayBooleanFilterOperationDescriptor CreateBooleanOperation(object operationKind)
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

        public static ArrayFilterFieldDescriptor New(
            IDescriptorContext context,
            IFilterConvention filterConventions,
            NameString name)
        {
            var descriptor = new ArrayFilterFieldDescriptor(
                context, filterConventions);

            descriptor.Name(name);
            return descriptor;
        }
    }
}
