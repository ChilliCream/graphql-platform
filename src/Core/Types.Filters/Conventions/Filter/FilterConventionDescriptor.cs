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
            FilterConventionDefaultOperationDescriptor> _defaultOperations;

        private readonly ConcurrentDictionary<FilterKind,
            FilterConventionTypeDescriptor> _configurations;

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

        public IFilterConventionTypeDescriptor Type(FilterKind kind)
        {
            return _configurations.GetOrAdd(
                kind, (FilterKind kind) =>
                    FilterConventionTypeDescriptor.New(this, kind));
        }

        public IFilterConventionDescriptor Ignore(FilterKind kind, bool ignore = true)
        {
            _configurations.GetOrAdd(
                kind, (FilterKind kind) =>
                    FilterConventionTypeDescriptor.New(this, kind))
                .Ignore(ignore);
            return this;
        }

        public FilterConventionDefinition CreateDefinition()
        {
            Array values = Enum.GetValues(typeof(FilterKind));
            foreach (FilterConventionTypeDescriptor descriptor in _configurations.Values)
            {
                FilterConventionTypeDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    Definition.TypeDefinitions[definition.FilterKind] = definition;
                    Definition.AllowedOperations[definition.FilterKind]
                        = definition.AllowedOperations;
                    Definition.ImplicitFilters.Add(definition.TryCreateFilter);
                }
            }

            foreach (FilterConventionDefaultOperationDescriptor descriptor
                in _defaultOperations.Values)
            {
                FilterConventionOperationDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    Definition.DefaultOperationDescriptions[definition.OperationKind]
                        = definition.Description;
                    Definition.DefaultOperationNames[definition.OperationKind] = definition.Name;
                }
            }
            return Definition;
        }

        public static FilterConventionDescriptor New() => new FilterConventionDescriptor();

        public IFilterConventionDefaultOperationDescriptor Operation(FilterOperationKind kind)
        {
            return _defaultOperations.GetOrAdd(
                kind, (FilterOperationKind kind) =>
                FilterConventionDefaultOperationDescriptor.New(this, kind));
        }
    }
}
