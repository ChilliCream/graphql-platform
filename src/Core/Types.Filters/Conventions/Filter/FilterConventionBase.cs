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

        public NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind)
        {
            return GetOrCreateConfiguration().Names[kind](definition, kind);
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
