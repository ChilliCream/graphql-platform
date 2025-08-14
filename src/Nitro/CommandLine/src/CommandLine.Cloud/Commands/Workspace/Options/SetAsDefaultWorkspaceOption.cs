namespace ChilliCream.Nitro.CLI;

public sealed class SetAsDefaultWorkspaceOption : Option<bool?>
{
    public SetAsDefaultWorkspaceOption() : base("--default")
    {
        Description = "Set the created workspace as the default workspace.";
    }
}
