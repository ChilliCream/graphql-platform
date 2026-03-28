namespace ChilliCream.Nitro.CommandLine.Options;

internal class WaitForApprovalOption : Option<bool>
{
    public WaitForApprovalOption() : base("--wait-for-approval")
    {
        Required = false;
        Description = "Wait for approval";
        this.DefaultFromEnvironmentValue("SUBGRAPH_NAME");
    }
}

internal sealed class OptionalWaitForApprovalOption : WaitForApprovalOption
{
    public OptionalWaitForApprovalOption() : base()
    {
        Required = false;
    }
}
