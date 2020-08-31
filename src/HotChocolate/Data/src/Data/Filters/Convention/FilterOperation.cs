namespace HotChocolate.Data.Filters
{
    public class FilterOperation
    {
        public FilterOperation(int id, NameString name , string? description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public int Id { get; }

        public NameString Name { get; }

        public string? Description { get; }

        internal static FilterOperation FromDefinition(
            FilterOperationConventionDefinition definition) =>
            new FilterOperation(definition.Id, definition.Name, definition.Description);
    }
}
