using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDescriptor
        : IFilterConventionDescriptor
    {
        private IFilterVisitorDescriptorBase<FilterVisitorDefinitionBase>? _visitorDescriptor;

        private readonly List<TryCreateImplicitFilter> _implicitFilters =
            new List<TryCreateImplicitFilter>();

        protected FilterConventionDescriptor()
        {
        }

        internal protected FilterConventionDefinition Definition { get; private set; } =
            new FilterConventionDefinition();

        private readonly Dictionary<FilterOperationKind,
            FilterConventionDefaultOperationDescriptor> _defaultOperations =
                new Dictionary<FilterOperationKind,
                    FilterConventionDefaultOperationDescriptor>();

        private readonly ConcurrentDictionary<FilterKind,
            FilterConventionTypeDescriptor> _configurations =
                new ConcurrentDictionary<FilterKind, FilterConventionTypeDescriptor>();

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

        public IFilterConventionDescriptor TypeName(
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

        public IFilterConventionDescriptor Description(
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

        public IFilterConventionDescriptor AddImplicitFilter(
            TryCreateImplicitFilter factory,
            int? position = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var insertAt = position ?? _implicitFilters.Count;
            _implicitFilters.Insert(insertAt, factory);

            return this;
        }

        public FilterConventionDefinition CreateDefinition()
        {
            if (_visitorDescriptor == null)
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                    .SetMessage("No visitor was defined for FilterConvention")
                    .Build());
            }

            var allowedOperations = 
                new Dictionary<FilterKind, IReadOnlyCollection<FilterOperationKind>>();
            var typeDefinitions = new Dictionary<FilterKind, FilterConventionTypeDefinition>();
            var defaultOperationNames = new Dictionary<FilterOperationKind, CreateFieldName>();
            var defaultOperationDescriptions = new Dictionary<FilterOperationKind, string>();

            foreach (FilterConventionTypeDescriptor descriptor in _configurations.Values)
            {
                FilterConventionTypeDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    typeDefinitions[definition.FilterKind] = definition;
                    allowedOperations[definition.FilterKind] = definition.AllowedOperations;
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
                        defaultOperationDescriptions[definition.OperationKind]
                            = definition.Description;
                    }

                    if (definition.Name != null)
                    {
                        defaultOperationNames[definition.OperationKind]
                            = definition.Name;
                    }
                }
            }
            
            Definition.DefaultOperationDescriptions = defaultOperationDescriptions;
            Definition.DefaultOperationNames = defaultOperationNames;
            Definition.AllowedOperations = allowedOperations;
            Definition.TypeDefinitions = typeDefinitions;
            Definition.ImplicitFilters = _implicitFilters.ToArray();
            Definition.VisitorDefinition = _visitorDescriptor.CreateDefinition();

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
