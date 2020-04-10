using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDescriptor
        : IFilterConventionDescriptor
    {
        private IFilterVisitorDescriptorBase<FilterVisitorDefinitionBase>? _visitorDescriptor = null;

        protected FilterConventionDescriptor()
        {
        }

        internal protected FilterConventionDefinition Definition { get; private set; } =
            new FilterConventionDefinition();

        private readonly ConcurrentDictionary<FilterOperationKind,
            FilterConventionDefaultOperationDescriptor> _defaultOperations
            = new ConcurrentDictionary<FilterOperationKind,
                FilterConventionDefaultOperationDescriptor>();

        private readonly ConcurrentDictionary<FilterKind,
            FilterConventionTypeDescriptor> _configurations
            = new ConcurrentDictionary<FilterKind, FilterConventionTypeDescriptor>();

        public IFilterConventionDescriptor ArgumentName(NameString argumentName)
        {
            Definition.ArgumentName = argumentName;
            return this;
        }

        public IFilterConventionDescriptor ElementName(
            NameString name)
        {
            Definition.ElementName = name;
            return this;
        }

        public IFilterConventionDescriptor FilterTypeName(
            GetFilterTypeName factory)
        {
            Definition.FilterTypeNameFactory = factory;
            return this;
        }

        public IFilterConventionDescriptor Visitor(
            IFilterVisitorDescriptorBase<FilterVisitorDefinitionBase> visitor)
        {
            _visitorDescriptor = visitor;
            return this;
        }

        public IFilterConventionDescriptor FilterTypeDescription(
            GetFilterTypeDescription factory)
        {
            Definition.FilterTypeDescriptionFactory = factory;
            return this;
        }

        public IFilterConventionDefaultOperationDescriptor Operation(FilterOperationKind kind)
        {
            return _defaultOperations.GetOrAdd(
                kind,
                (FilterOperationKind kind) =>
                    FilterConventionDefaultOperationDescriptor.New(this, kind));
        }

        public IFilterConventionTypeDescriptor Type(FilterKind kind)
        {
            return _configurations.GetOrAdd(
                kind,
                (FilterKind kind) => FilterConventionTypeDescriptor.New(this, kind));
        }

        public IFilterConventionDescriptor Ignore(FilterKind kind, bool ignore = true)
        {
            _configurations.GetOrAdd(
                kind,
                (FilterKind kind) => FilterConventionTypeDescriptor.New(this, kind))
                .Ignore(ignore);
            return this;
        }

        public FilterConventionDefinition CreateDefinition()
        {
            foreach (FilterConventionTypeDescriptor descriptor in _configurations.Values)
            {
                FilterConventionTypeDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    Definition.TypeDefinitions[definition.FilterKind] = definition;
                    Definition.AllowedOperations[definition.FilterKind]
                        = definition.AllowedOperations;

                    if (definition.TryCreateFilter != null)
                    {
                        Definition.ImplicitFilters.Add(definition.TryCreateFilter);
                    }
                }
            }

            foreach (FilterConventionDefaultOperationDescriptor descriptor
                in _defaultOperations.Values)
            {
                FilterConventionOperationDefinition definition = descriptor.CreateDefinition();

                if (!definition.Ignore)
                {
                    if (definition.Description != null)
                    {
                        Definition.DefaultOperationDescriptions[definition.OperationKind]
                            = definition.Description;
                    }

                    if (definition.Name != null)
                    {
                        Definition.DefaultOperationNames[definition.OperationKind]
                            = definition.Name;
                    }
                }
            }

            if (_visitorDescriptor != null)
            {
                _visitorDescriptor.Convention(Definition);
                Definition.VisitorDefinition = _visitorDescriptor.CreateDefinition();
            }

            return Definition;
        }

        public IFilterConventionDescriptor Reset()
        {
            Definition = new FilterConventionDefinition();
            _defaultOperations.Clear();
            _configurations.Clear();
            return this;
        }

        public static FilterConventionDescriptor New() => new FilterConventionDescriptor();
    }
}
