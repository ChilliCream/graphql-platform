using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        private readonly ConcurrentDictionary<int,
            FilterConventionDefaultOperationDescriptor> _defaultOperations =
                new ConcurrentDictionary<int,
                    FilterConventionDefaultOperationDescriptor>();

        private readonly ConcurrentDictionary<int,
            FilterConventionTypeDescriptor> _configurations =
                new ConcurrentDictionary<int, FilterConventionTypeDescriptor>();

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

        public IFilterConventionDefaultOperationDescriptor Operation(int kind)
        {
            return _defaultOperations.GetOrAdd(
                kind,
                (int kind) =>
                FilterConventionDefaultOperationDescriptor.New(this, kind));
        }

        public IFilterConventionTypeDescriptor Type(int kind)
        {
            return _configurations.GetOrAdd(
                kind,
                (int kind) => FilterConventionTypeDescriptor.New(this, kind));
        }

        public IFilterConventionDescriptor Ignore(int kind, bool ignore = true)
        {
            _configurations.GetOrAdd(
                kind,
                (int kind) => FilterConventionTypeDescriptor.New(this, kind))
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
                new Dictionary<int, IReadOnlyCollection<int>>();
            var typeDefinitions = new Dictionary<int, FilterConventionTypeDefinition>();
            var defaultOperationNames = new Dictionary<int, CreateFieldName>();
            var defaultOperationDescriptions = new Dictionary<int, string>();

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

        public bool TryGetVisitor(
            [NotNullWhen(true)] out FilterVisitorDescriptorBase? visitor)
        {
            if (_visitorDescriptor != null)
            {
                visitor = _visitorDescriptor;
                return true;
            }
            visitor = null;
            return false;
        }
    }
}
