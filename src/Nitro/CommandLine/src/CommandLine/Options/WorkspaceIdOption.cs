namespace ChilliCream.Nitro.CommandLine.Options;

internal class WorkspaceIdOption : Option<string>
{
    public const string OptionName = "--workspace-id";

    public WorkspaceIdOption() : base(OptionName, "The ID of the workspace.")
    {
        IsRequired = false;
        this.DefaultFromEnvironmentValue("WORKSPACE_ID");
    }
}
