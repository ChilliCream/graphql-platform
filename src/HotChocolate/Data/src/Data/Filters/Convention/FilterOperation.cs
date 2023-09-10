namespace HotChocolate.Data.Filters;

public class FilterOperation
{
    public FilterOperation(int id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public int Id { get; }

    public string Name { get; }

    public string? Description { get; }

    internal static FilterOperation FromDefinition(
        FilterOperationConventionDefinition definition) =>
        new(definition.Id, definition.Name, definition.Description);
}
