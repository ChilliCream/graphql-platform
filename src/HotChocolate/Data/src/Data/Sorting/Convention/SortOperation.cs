namespace HotChocolate.Data.Sorting;

public class SortOperation
{
    public SortOperation(int id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public int Id { get; }

    public string Name { get; }

    public string? Description { get; }

    internal static SortOperation FromDefinition(
        SortOperationConventionDefinition definition) =>
        new SortOperation(definition.Id, definition.Name, definition.Description);
}
