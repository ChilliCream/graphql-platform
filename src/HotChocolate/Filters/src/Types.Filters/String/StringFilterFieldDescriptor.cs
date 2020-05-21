using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class StringFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IStringFilterFieldDescriptor
    {
        private static readonly IClrTypeReference _clrTypeReference =
            new ClrTypeReference(typeof(string), TypeContext.Input).Compile();

        public StringFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            IFilterConvention filterConventions)
            : base(FilterKind.String, context, property, filterConventions)
        {
        }

        public StringFilterFieldDescriptor(
            IDescriptorContext context,
            IFilterConvention filterConventions)
            : base(FilterKind.String, context, filterConventions)
        {
            Definition.Type = _clrTypeReference;
        }

        /// <inheritdoc/>
        public new IStringFilterFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowEquals()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotEquals()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowContains()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Contains);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotContains()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotContains);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowIn()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotIn()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowStartsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.StartsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotStartsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotStartsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowEndsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.EndsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterOperationDescriptor AllowNotEndsWith()
        {
            StringFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEndsWith);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IStringFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();

        private StringFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                    () => CreateOperation(operationKind));
        }

        private StringFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(string),
                Definition.Kind,
                operationKind,
                Definition.Property);

            return StringFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation,
                FilterConvention);
        }

        public static StringFilterFieldDescriptor New(
            IDescriptorContext context,
            IFilterConvention convention,
            NameString name)
        {
            var descriptor = new StringFilterFieldDescriptor(context, convention);
            descriptor.Name(name);
            return descriptor;
        }
    }
}
