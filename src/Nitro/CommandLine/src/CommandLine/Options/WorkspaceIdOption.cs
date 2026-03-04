namespace ChilliCream.Nitro.CommandLine.Options;

internal class WorkspaceIdOption : Option<string>
{
    public WorkspaceIdOption() : base("--workspace-id", "The ID of the workspace.")
    {
        IsRequired = false;
        this.DefaultFromEnvironmentValue("WORKSPACE_ID");
    }
}
