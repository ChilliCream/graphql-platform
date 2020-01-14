namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionOperationDescriptorBase
        : IFilterConventionOperationDescriptorBase
    {
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

        public FilterConventionOperationDefinition CreateDefinition() => Definition;

    }
}
