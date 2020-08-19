namespace HotChocolate.Data.Filters
{
    public class FilterOperationConventionDescriptor : IFilterOperationConventionDescriptor
    {
        public FilterOperationConventionDescriptor(int id)
        {
            Definition.Id = id;
        }

        internal FilterOperationConventionDefinition Definition { get; } =
            new FilterOperationConventionDefinition();

        public FilterOperationConventionDefinition CreateDefinition() => Definition;

        public IFilterOperationConventionDescriptor Name(string name)
        {
            Definition.Name = name;
            return this;
        }

        public IFilterOperationConventionDescriptor Description(string description)
        {
            Definition.Description = description;
            return this;
        }

        public static FilterOperationConventionDescriptor New(int operation) =>
            new FilterOperationConventionDescriptor(operation);
    }
}
