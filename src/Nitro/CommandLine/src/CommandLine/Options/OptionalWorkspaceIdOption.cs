namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalWorkspaceIdOption : WorkspaceIdOption
{
    public OptionalWorkspaceIdOption()
    {
        IsRequired = false;
    }
}
