namespace ChilliCream.Nitro.CommandLine.Options;

internal class WorkspaceIdOption : Option<string>
{
    public const string OptionName = "--workspace-id";

    public WorkspaceIdOption() : base(OptionName)
    {
        Description = "The ID of the workspace.";
        Required = false;
        this.DefaultFromEnvironmentValue("WORKSPACE_ID");
    }
}
