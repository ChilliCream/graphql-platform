using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class ObjectFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IObjectFilterFieldDescriptor
    {
        private readonly Type _type;

        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type)
            : base(context, property)
        {
            _type = type;
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Object
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
                _type,
                operationKind,
                Definition.Property);

            ExtendedTypeReference typeRef = Context.TypeInspector.GetTypeRef(
                typeof(FilterInputType<>).MakeGenericType(_type),
                Definition.Type.Context);

            return ObjectFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                typeRef,
                operation);
        }

        private ObjectFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
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

    }
}
