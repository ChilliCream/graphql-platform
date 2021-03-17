using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class ComparableFilterFieldDescriptor
        : FilterFieldDescriptorBase
        , IComparableFilterFieldDescriptor
    {
        public ComparableFilterFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property)
        {
            AllowedOperations = new HashSet<FilterOperationKind>
            {
                FilterOperationKind.Equals,
                FilterOperationKind.NotEquals,

                FilterOperationKind.In,
                FilterOperationKind.NotIn,

                FilterOperationKind.GreaterThan,
                FilterOperationKind.NotGreaterThan,

                FilterOperationKind.GreaterThanOrEquals,
                FilterOperationKind.NotGreaterThanOrEquals,

                FilterOperationKind.LowerThan,
                FilterOperationKind.NotLowerThan,

                FilterOperationKind.LowerThanOrEquals,
                FilterOperationKind.NotLowerThanOrEquals
            };
        }

        protected override ISet<FilterOperationKind> AllowedOperations { get; }

        /// <inheritdoc/>
        public new IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior)
        {
            base.BindFilters(bindingBehavior);
            return this;
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor BindFiltersExplicitly() =>
            BindFilters(BindingBehavior.Explicit);

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor BindFiltersImplicitly() =>
            BindFilters(BindingBehavior.Implicit);

        /// <inheritdoc/>
        public new IComparableFilterFieldDescriptor Type<TLeafType>()
            where TLeafType : class, ILeafType
        {
            base.Type<TLeafType>();
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterFieldDescriptor Type<TLeafType>(TLeafType leafType)
            where TLeafType : class, ILeafType
        {
            base.Type<TLeafType>(leafType);
            return this;
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor Type(NamedTypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        /// <inheritdoc/>
        public new IComparableFilterFieldDescriptor Type(Type type)
        {
            Type extractedType = Context.TypeInspector.ExtractNamedType(type);

            if (Context.TypeInspector.IsSchemaType(extractedType)
                && !typeof(ILeafType).IsAssignableFrom(extractedType))
            {
                // TODO : resource
                throw new ArgumentException(
                    "TypeResources.ObjectFieldDescriptorBase_FieldType");
            }

            Definition.SetMoreSpecificType(
                Context.TypeInspector.GetType(extractedType),
                TypeContext.Input);

            return this;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowEquals()
        {
            ComparableFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.Equals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotEquals()
        {
            ComparableFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowIn()
        {
            ComparableFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.In);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotIn()
        {
            ComparableFilterOperationDescriptor field =
                GetOrCreateOperation(FilterOperationKind.NotIn);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowGreaterThan()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.GreaterThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotGreaterThan()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.NotGreaterThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowGreaterThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.GreaterThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotGreaterThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.NotGreaterThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowLowerThan()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.LowerThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotLowerThan()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.NotLowerThan);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowLowerThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.LowerThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterOperationDescriptor AllowNotLowerThanOrEquals()
        {
            ComparableFilterOperationDescriptor field =
                 GetOrCreateOperation(FilterOperationKind.NotLowerThanOrEquals);
            Filters.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public IComparableFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        protected override FilterOperationDefintion CreateOperationDefinition(
            FilterOperationKind operationKind) =>
            CreateOperation(operationKind).CreateDefinition();


        private ComparableFilterOperationDescriptor GetOrCreateOperation(
            FilterOperationKind operationKind)
        {
            return Filters.GetOrAddOperation(operationKind,
                () => CreateOperation(operationKind));
        }

        private ComparableFilterOperationDescriptor CreateOperation(
            FilterOperationKind operationKind)
        {
            var operation = new FilterOperation(
                typeof(IComparable),
                operationKind,
                Definition.Property);

            return ComparableFilterOperationDescriptor.New(
                Context,
                this,
                CreateFieldName(operationKind),
                RewriteType(operationKind),
                operation);
        }
    }
}
