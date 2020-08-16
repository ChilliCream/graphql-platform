namespace HotChocolate.Data.Filters
{
    public class OperationConvention
    {
        internal OperationConvention(
            FilterOperationConventionDefinition definition)
        {
            Name = definition.Name;
            Description = definition.Description;
            Operation = definition.Operation;
        }

        public string Name { get; }

        public string? Description { get; }

        public int Operation { get; }
    }
}
