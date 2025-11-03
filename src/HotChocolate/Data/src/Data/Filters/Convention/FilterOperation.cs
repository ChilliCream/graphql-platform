namespace HotChocolate.Data.Filters;

public class FilterOperation(int id, string name, string? description)
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public string? Description { get; } = description;

    internal static FilterOperation FromConfiguration(
        FilterOperationConventionConfiguration configuration) =>
        new(configuration.Id, configuration.Name, configuration.Description);
}
