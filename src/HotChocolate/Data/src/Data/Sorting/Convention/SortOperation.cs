using HotChocolate.Utilities;

namespace HotChocolate.Data.Sorting;

public class SortOperation(int id, string name, string? description)
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public string? Description { get; } = description;

    internal static SortOperation FromConfiguration(
        SortOperationConventionConfiguration configuration)
        => new(configuration.Id, configuration.Name.EnsureGraphQLName(), configuration.Description);
}
