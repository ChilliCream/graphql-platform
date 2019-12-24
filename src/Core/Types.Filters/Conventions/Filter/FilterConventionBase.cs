using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Conventions
{
    public delegate bool TryCreateImplicitFilter(
            PropertyInfo property,
            out FilterFieldDefintion definition);

    public delegate NameString CreateFieldName(
            FilterFieldDefintion definition,
            FilterOperationKind kind);

    public delegate NameString GetFilterTypeName(
        IDescriptorContext context,
        Type entityType);

    public class FilterConventionBase : IFilterConvention
    {
        private readonly Action<IFilterConventionDescriptor> _configure;
        private FilterConventionDefinition _definition;

        public FilterConventionBase()
        {
            _configure = Configure;
        }
        public NameString GetArgumentName()
        {
            return GetOrCreateConfiguration().ArgumentName;
        }

        public NameString GetArrayFilterPropertyName()
        {
            return GetOrCreateConfiguration().ArrayFilterPropertyName;
        }

        public NameString CreateFieldName(
            FilterKind filterKind, FilterFieldDefintion definition, FilterOperationKind kind)
        {
            FilterConventionDefinition configuration = GetOrCreateConfiguration();

            if (configuration.TypeDefinitions.TryGetValue(filterKind,
                    out FilterConventionTypeDefinition typeDefinition) &&
                    typeDefinition.OperationNames.TryGetValue(kind,
                        out CreateFieldName createFieldName)
                )
            {
                return createFieldName(definition, kind);
            }

            if (configuration.DefaultOperationNames.TryGetValue(kind,
                        out CreateFieldName createDefaultFieldName))
            {
                return createDefaultFieldName(definition, kind);
            }

            // Todo resources && set code
            throw new SchemaException(
                SchemaErrorBuilder.New()
                .SetMessage(
                    string.Format(
                        "No operation name for {0} in filter type {1} found. Add operation"
                        + "name to filter conventions",
                        kind,
                        filterKind))
                .Build());
        }

        public virtual NameString GetFilterTypeName(IDescriptorContext context, Type entityType)
        {
            return GetOrCreateConfiguration().GetFilterTypeName(context, entityType);
        }

        public IEnumerable<TryCreateImplicitFilter> GetImplicitFilterFactories()
        {
            return GetOrCreateConfiguration().ImplicitFilters;
        }

        protected virtual void Configure(
            IFilterConventionDescriptor descriptor)
        {
        }

        protected FilterConventionDefinition CreateDefinition()
        {
            var descriptor = FilterConventionDescriptor.New();
            ApplyDefaultConfiguration(descriptor);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        private FilterConventionDefinition GetOrCreateConfiguration()
        {
            lock (_definition)
            {
                if (_definition == null)
                {
                    _definition = CreateDefinition();
                }
                return _definition;
            }
        }

        private void ApplyDefaultConfiguration(IFilterConventionDescriptor descriptor)
        {
            descriptor.ArgumentName("where")
                .ArrayFilterPropertyName("element")
                .Type(FilterKind.Array)
                    .Operation(FilterOperationKind.ArrayAll).And()
                    .Operation(FilterOperationKind.ArrayAny).And()
                    .Operation(FilterOperationKind.ArraySome).And()
                    .Operation(FilterOperationKind.ArrayNone).And()
                    .And()
                .Type(FilterKind.Boolean)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .And()
                .Type(FilterKind.Comparable)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .Operation(FilterOperationKind.In).And()
                    .Operation(FilterOperationKind.NotIn).And()
                    .Operation(FilterOperationKind.GreaterThan).And()
                    .Operation(FilterOperationKind.NotGreaterThan).And()
                    .Operation(FilterOperationKind.GreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.NotGreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.LowerThan).And()
                    .Operation(FilterOperationKind.NotLowerThan).And()
                    .Operation(FilterOperationKind.LowerThanOrEquals).And()
                    .Operation(FilterOperationKind.NotLowerThanOrEquals).And()
                    .And()
                .Type(FilterKind.Object)
                    .Operation(FilterOperationKind.Equals).And()
                    .And()
                .Type(FilterKind.String)
                    .Operation(FilterOperationKind.Equals).And()
                    .Operation(FilterOperationKind.NotEquals).And()
                    .Operation(FilterOperationKind.Contains).And()
                    .Operation(FilterOperationKind.NotContains).And()
                    .Operation(FilterOperationKind.StartsWith).And()
                    .Operation(FilterOperationKind.NotStartsWith).And()
                    .Operation(FilterOperationKind.EndsWith).And()
                    .Operation(FilterOperationKind.NotEndsWith).And()
                    .Operation(FilterOperationKind.In).And()
                    .Operation(FilterOperationKind.NotIn).And();
        }
    }
}
