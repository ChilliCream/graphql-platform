using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionOperationDescriptor : IFilterConventionOperationDescriptor
    {
        private readonly FilterConventionDescriptor _descriptor;

        protected FilterConventionOperationDescriptor(
            FilterConventionDescriptor descriptor,
            FilterOperationKind kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.OperationKind = kind;
        }

        internal protected FilterConventionOperationDefinition Definition { get; } =
            new FilterConventionOperationDefinition();

        public IFilterConventionDescriptor And()
        {
            return _descriptor;
        }

        public IFilterConventionOperationDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }


        public IFilterConventionOperationDescriptor Name(CreateFieldName factory)
        {
            Definition.Name = factory;
            return this;
        }

        public IFilterConventionOperationDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public IFilterConventionOperationDescriptor TryCreateImplicitFilter(
            TryCreateImplicitFilter factory)
        {
            Definition.TryCreateFilter = factory;
            return this;
        }

        public IFilterConventionOperationDescriptor AllowedFilter(AllowedFilterType value)
        {
            Array values = Enum.GetValues(typeof(AllowedFilterType));
            foreach (AllowedFilterType item in values)
            {
                if (value.HasFlag(item))
                {
                    Definition.AllowedFilters.Add(item);
                }
            }
            return this;
        }

        public FilterConventionOperationDefinition CreateDefinition() => Definition;


        public static FilterConventionOperationDescriptor New(
            FilterConventionDescriptor descriptor, FilterOperationKind kind) =>
                new FilterConventionOperationDescriptor(descriptor, kind);
    }
}
