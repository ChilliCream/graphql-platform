using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters;

public class FilterOperationConventionDefinition
{
    private string _name = string.Empty;

    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }
}
