namespace HotChocolate.AspNetCore.Authorization;

public sealed class GraphQl
{
    public string Policy { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
