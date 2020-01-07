using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDescriptor : IFilterConventionTypeDescriptor
    {
        private readonly FilterConventionDescriptor _descriptor;

        protected FilterConventionTypeDescriptor(
            FilterConventionDescriptor descriptor,
            FilterKind kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.FilterKind = kind;
        }

        internal protected FilterConventionTypeDefinition Definition { get; } =
            new FilterConventionTypeDefinition();

        private readonly ConcurrentDictionary<FilterOperationKind,
            FilterConventionOperationDescriptor> _operations
            = new ConcurrentDictionary<FilterOperationKind, FilterConventionOperationDescriptor>();

        public IFilterConventionDescriptor And()
        {
            return _descriptor;
        }

        public IFilterConventionTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }


        public IFilterConventionTypeDescriptor Name(NameString factory)
        {
            Definition.Name = factory;
            return this;
        }

        public IFilterConventionTypeDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public IFilterConventionTypeDescriptor Ignore(FilterOperationKind kind, bool ignore = true)
        {
            _operations.GetOrAdd(
                    kind, (FilterOperationKind kind) =>
                        FilterConventionOperationDescriptor.New(this, kind))
                .Ignore(true);

            return this;
        }

        public IFilterConventionOperationDescriptor Operation(FilterOperationKind kind)
        {
            return _operations.GetOrAdd(
                kind, (FilterOperationKind kind) =>
                    FilterConventionOperationDescriptor.New(this, kind));
        }

        public IFilterConventionTypeDescriptor TryCreateImplicitFilter(
            TryCreateImplicitFilter factory)
        {
            Definition.TryCreateFilter = factory;
            return this;
        }

        public FilterConventionTypeDefinition CreateDefinition()
        {
            foreach (FilterConventionOperationDescriptor descriptor in _operations.Values)
            {
                FilterConventionOperationDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    if (definition.Description != null)
                    {
                        Definition.OperationDescriptions[definition.OperationKind]
                        = definition.Description;
                    }

                    if (definition.Name != null)
                    {
                        Definition.OperationNames[definition.OperationKind]
                            = definition.Name;
                    }
                    Definition.AllowedOperations.Add(definition.OperationKind);
                }
            }
            return Definition;
        }



        public static FilterConventionTypeDescriptor New(
            FilterConventionDescriptor descriptor, FilterKind kind) =>
                new FilterConventionTypeDescriptor(descriptor, kind);
    }
}
