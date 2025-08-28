namespace ChilliCream.Nitro.CommandLine.Cloud;

public sealed class WorkspaceNameOption : Option<string>
{
    public WorkspaceNameOption() : base("--name")
    {
        Description = "The name of the workspace";
        IsRequired = false;
    }
}
