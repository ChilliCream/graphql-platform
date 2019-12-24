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
            descriptor.UseDefault();
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
    }
}
