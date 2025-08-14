namespace ChilliCream.Nitro.CLI.Option;

internal class WaitForApprovalOption : Option<bool>
{
    public WaitForApprovalOption() : base("--wait-for-approval", "Wait for approval")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_NAME");
    }
}

internal sealed class OptionalWaitForApprovalOption : WaitForApprovalOption
{
    public OptionalWaitForApprovalOption() : base()
    {
        IsRequired = false;
    }
}
