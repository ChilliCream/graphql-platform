namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal class WorkspaceIdOption : Option<string>
{
    public WorkspaceIdOption() : base("--workspace-id", "The id of the workspace.")
    {
        IsRequired = false;
        this.DefaultFromEnvironmentValue("WORKSPACE_ID");
    }
}
