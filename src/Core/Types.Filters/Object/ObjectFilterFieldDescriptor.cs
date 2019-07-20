using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterFieldDescriptor<TObject>
        : FilterFieldDescriptorBase
        , IObjectFilterFieldDescriptor<TObject>
    {
        public ObjectFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
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
        public IObjectFilterFieldDescriptor<TObject> BindExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IObjectFilterFieldDescriptor<TObject> BindImplicitly() =>
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
            var desc = FilterInputTypeDescriptor<TObject>.New(Context, typeof(TObject));
            descriptor.Invoke(desc);

            // TODO: I don't know how to initialize this type 

            

            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return this;
        }

        public IObjectFilterFieldDescriptor<TObject> AllowObject<TFilter>() where TFilter : IFilterInputTypeDescriptor<TObject>
        {

            // TODO: Do wee need to pass the generic TFilter to create Option?
            ObjectFilterOperationDescriptor<TObject> field =
                CreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
        }
    }
}
