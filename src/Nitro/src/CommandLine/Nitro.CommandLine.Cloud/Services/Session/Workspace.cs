namespace ChilliCream.Nitro.CLI.Auth;

internal sealed class Workspace(string id, string name)
{
    public string Id { get; set; } = id;

    public string Name { get; set; } = name;
}
