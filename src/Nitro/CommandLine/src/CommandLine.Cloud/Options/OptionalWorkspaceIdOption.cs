namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class OptionalWorkspaceIdOption : WorkspaceIdOption
{
    public OptionalWorkspaceIdOption()
    {
        IsRequired = false;
    }
}
