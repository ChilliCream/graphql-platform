using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDescriptor : IFilterConventionTypeDescriptor
    {
        private readonly FilterConventionDescriptor _descriptor;

        protected FilterConventionTypeDescriptor(
            FilterConventionDescriptor descriptor,
            object kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.FilterKind = kind;
        }

        internal protected FilterConventionTypeDefinition Definition { get; } =
            new FilterConventionTypeDefinition();

        private readonly ConcurrentDictionary<object, FilterConventionOperationDescriptor>
            _operations =
                new ConcurrentDictionary<object, FilterConventionOperationDescriptor>();

        public IFilterConventionDescriptor And()
        {
            return _descriptor;
        }

        public IFilterConventionTypeDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public IFilterConventionTypeDescriptor Ignore(
            object kind,
            bool ignore = true)
        {
            _operations.GetOrAdd(kind, kind => FilterConventionOperationDescriptor.New(this, kind))
                .Ignore(true);

            return this;
        }

        public IFilterConventionOperationDescriptor Operation(object kind)
        {
            return _operations.GetOrAdd(
                kind,
                kind => FilterConventionOperationDescriptor.New(this, kind));
        }

        public FilterConventionTypeDefinition CreateDefinition()
        {
            var operationDescriptions = new Dictionary<object, string>();
            var operationsNames = new Dictionary<object, CreateFieldName>();
            var allowedOperations = new HashSet<object>();

            foreach (FilterConventionOperationDescriptor descriptor in _operations.Values)
            {
                FilterConventionOperationDefinition definition = descriptor.CreateDefinition();
                if (!definition.Ignore)
                {
                    if (definition.Description != null)
                    {
                        operationDescriptions[definition.OperationKind] = definition.Description;
                    }

                    if (definition.Name != null)
                    {
                        operationsNames[definition.OperationKind] = definition.Name;
                    }
                    allowedOperations.Add(definition.OperationKind);
                }
            }

            Definition.AllowedOperations = allowedOperations;
            Definition.OperationDescriptions = operationDescriptions;
            Definition.OperationNames = operationsNames;
            return Definition;
        }

        public static FilterConventionTypeDescriptor New(
            FilterConventionDescriptor descriptor, object kind) =>
            new FilterConventionTypeDescriptor(descriptor, kind);
    }
}
