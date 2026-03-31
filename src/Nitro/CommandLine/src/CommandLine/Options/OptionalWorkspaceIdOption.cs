namespace ChilliCream.Nitro.CommandLine.Options;

internal class OptionalWorkspaceIdOption : Option<string>
{
    public const string OptionName = "--workspace-id";

    public OptionalWorkspaceIdOption() : base(OptionName)
    {
        Description = "The ID of the workspace";
        Required = false;
        this.DefaultFromEnvironmentValue("WORKSPACE_ID");
    }
}
