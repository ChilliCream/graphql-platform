namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class WorkspaceCommand : Command
{
    public WorkspaceCommand() : base("workspace")
    {
        Description = "Use this command to manage workspaces";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateWorkspaceCommand());
        AddCommand(new CurrentWorkspaceCommand());
        AddCommand(new SetDefaultWorkspaceCommand());
        AddCommand(new ListWorkspaceCommand());
        AddCommand(new ShowWorkspaceCommand());
    }
}
