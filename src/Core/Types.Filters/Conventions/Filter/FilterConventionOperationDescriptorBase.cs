using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public abstract class FilterConventionOperationDescriptorBase
        : IFilterConventionOperationDescriptorBase
    {
        private readonly FilterConventionTypeDescriptor _descriptor;

        protected FilterConventionOperationDescriptorBase( 
            FilterOperationKind kind)
        { 
            Definition.OperationKind = kind;
        }

        internal protected FilterConventionOperationDefinition Definition { get; } =
            new FilterConventionOperationDefinition();


        public IFilterConventionOperationDescriptorBase Description(string value)
        {
            Definition.Description = value;
            return this;
        }


        public IFilterConventionOperationDescriptorBase Name(CreateFieldName factory)
        {
            Definition.Name = factory;
            return this;
        }

        public IFilterConventionOperationDescriptorBase Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public FilterConventionOperationDefinition CreateDefinition() => Definition;

    }
}
