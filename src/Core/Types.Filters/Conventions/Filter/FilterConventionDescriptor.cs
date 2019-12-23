using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDescriptor : IFilterConventionDescriptor
    {
        protected FilterConventionDescriptor()
        {
        }

        internal protected FilterConventionDefinition Definition { get; } =
            new FilterConventionDefinition();

        private readonly ConcurrentDictionary<FilterOperationKind,
            FilterConventionOperationDescriptor> _configurations;

        public IFilterConventionDescriptor ArgumentName(NameString argumentName)
        {
            Definition.ArgumentName = argumentName;
            return this;
        }

        public IFilterConventionDescriptor ArrayFilterPropertyName(
            NameString arrayFilterPropertyName)
        {
            Definition.ArrayFilterPropertyName = arrayFilterPropertyName;
            return this;
        }

        public IFilterConventionDescriptor GetFilterTypeName(GetFilterTypeName getFilterTypeName)
        {
            Definition.GetFilterTypeName = getFilterTypeName;
            return this;
        }

        public IFilterConventionOperationDescriptor Filter(FilterOperationKind kind)
        {
            return _configurations.GetOrAdd(
                kind, (FilterOperationKind kind) =>
                FilterConventionOperationDescriptor.New(this, kind));
        }

        public FilterConventionDefinition CreateDefinition()
        {
            Array values = Enum.GetValues(typeof(AllowedFilterType));
            foreach (FilterConventionOperationDescriptor descriptor in _configurations.Values)
            {
                FilterConventionOperationDefinition defintion = descriptor.CreateDefinition();
                if (!defintion.Ignore)
                {
                    Definition.Descriptions[defintion.OperationKind] = defintion.Description;
                    Definition.Names[defintion.OperationKind] = defintion.Name;
                    Definition.ImplicitFilters.Add(defintion.TryCreateFilter);
                    foreach (AllowedFilterType item in defintion.AllowedFilters)
                    {
                        Definition.AllowedOperations[item] = defintion.OperationKind;
                    }
                }
            }
            return Definition;
        }

        public static FilterConventionDescriptor New() => new FilterConventionDescriptor();
    }
}
