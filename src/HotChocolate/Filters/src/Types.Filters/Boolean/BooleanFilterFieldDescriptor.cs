using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IBooleanFilterFieldDescriptor
    {
        private static readonly IClrTypeReference _clrTypeReference =
            new ClrTypeReference(typeof(bool), TypeContext.Input).Compile();

        public BooleanFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(FilterKind.Boolean, context, property, filterConventions)
        {
        }

        public BooleanFilterFieldDescriptor(
            IDescriptorContext context,
            IFilterConvention filterConventions)
            : base(FilterKind.Boolean, context, filterConventions)
        {
            Definition.Type = _clrTypeReference;
        }

        /// <inheritdoc/>
        public new IBooleanFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public IBooleanFilterOperationDescriptor AllowEquals()
        {
            BooleanFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IBooleanFilterOperationDescriptor AllowNotEquals()
        {
            BooleanFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IBooleanFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            int operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private BooleanFilterOperationDescriptor GetOrCreateOperation(
            int operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        private BooleanFilterOperationDescriptor CreateOperation(
            int operationKind)
        {
            var operation = new FilterOperation(
                typeof(bool),
                Definition.Kind,
                operationKind,
                Definition.Property);

            return BooleanFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation,
                FilterConvention);
        }

        public static BooleanFilterFieldDescriptor New(
            IDescriptorContext context,
            IFilterConvention filterConventions,
            NameString name)
        {
            var descriptor = new BooleanFilterFieldDescriptor(
                context, filterConventions);

            descriptor.Name(name);
            return descriptor;
        }
    }
}
