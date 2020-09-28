namespace HotChocolate.Data.Filters
{
    public class FilterOperationConventionDescriptor : IFilterOperationConventionDescriptor
    {
        protected FilterOperationConventionDescriptor(int operationId)
        {
            Definition.Id = operationId;
        }

        protected FilterOperationConventionDefinition Definition { get; } =
            new FilterOperationConventionDefinition();

        public FilterOperationConventionDefinition CreateDefinition() =>
            Definition;

        /// <inheritdoc />
        public IFilterOperationConventionDescriptor Name(string name)
        {
            Definition.Name = name;
            return this;
        }

        /// <inheritdoc />
        public IFilterOperationConventionDescriptor Description(string description)
        {
            Definition.Description = description;
            return this;
        }

        public static FilterOperationConventionDescriptor New(int operationId) =>
            new FilterOperationConventionDescriptor(operationId);
    }
}
