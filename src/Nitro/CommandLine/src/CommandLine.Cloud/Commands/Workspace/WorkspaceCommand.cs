namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class WorkspaceCommand : Command
{
    public WorkspaceCommand() : base("workspace")
    {
        Description = "Manage workspaces";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateWorkspaceCommand());
        AddCommand(new CurrentWorkspaceCommand());
        AddCommand(new SetDefaultWorkspaceCommand());
        AddCommand(new ListWorkspaceCommand());
        AddCommand(new ShowWorkspaceCommand());
    }
}
