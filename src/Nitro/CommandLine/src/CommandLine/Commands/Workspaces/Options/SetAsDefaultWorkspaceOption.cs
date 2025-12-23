namespace ChilliCream.Nitro.CommandLine.Commands.Workspaces.Options;

public sealed class SetAsDefaultWorkspaceOption : Option<bool?>
{
    public SetAsDefaultWorkspaceOption() : base("--default")
    {
        Description = "Set the created workspace as the default workspace.";
    }
}
