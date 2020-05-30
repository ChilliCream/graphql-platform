using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDescriptor
        : IFilterConventionDescriptor
    {
        private FilterVisitorDescriptorBase? _visitorDescriptor;

        private readonly List<TryCreateImplicitFilter> _implicitFilters =
            new List<TryCreateImplicitFilter>();

        protected FilterConventionDescriptor()
        {
        }

        internal protected FilterConventionDefinition Definition { get; private set; } =
            new FilterConventionDefinition();

        private readonly ConcurrentDictionary<object,
            FilterConventionDefaultOperationDescriptor> _defaultOperations =
                new ConcurrentDictionary<object,
                    FilterConventionDefaultOperationDescriptor>();

        private readonly ConcurrentDictionary<object,
            FilterConventionTypeDescriptor> _configurations =
                new ConcurrentDictionary<object, FilterConventionTypeDescriptor>();

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
            FilterVisitorDescriptorBase visitor)
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

        public IFilterConventionDefaultOperationDescriptor Operation(object kind)
        {
            return _defaultOperations.GetOrAdd(
                kind,
                (object kind) =>
                FilterConventionDefaultOperationDescriptor.New(this, kind));
        }

        public IFilterConventionTypeDescriptor Type(object kind)
        {
            return _configurations.GetOrAdd(
                kind,
                (object kind) => FilterConventionTypeDescriptor.New(this, kind));
        }

        public IFilterConventionDescriptor Ignore(object kind, bool ignore = true)
        {
            _configurations.GetOrAdd(
                kind,
                (object kind) => FilterConventionTypeDescriptor.New(this, kind))
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
                new Dictionary<object, IReadOnlyCollection<object>>();
            var typeDefinitions = new Dictionary<object, FilterConventionTypeDefinition>();
            var defaultOperationNames = new Dictionary<object, CreateFieldName>();
            var defaultOperationDescriptions = new Dictionary<object, string>();

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
