namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces.Options;

public sealed class WorkspaceNameOption : Option<string>
{
    public WorkspaceNameOption() : base("--name")
    {
        Description = "The name of the workspace";
        IsRequired = false;
    }
}
