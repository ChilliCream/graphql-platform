namespace ChilliCream.Nitro.CommandLine;

internal class WaitForApprovalOption : Option<bool>
{
    public WaitForApprovalOption() : base("--wait-for-approval")
    {
        Required = false;
        Description = "Wait for the deployment to be approved before completing";
        this.DefaultFromEnvironmentValue(EnvironmentVariables.WaitForApproval);
    }
}

internal sealed class OptionalWaitForApprovalOption : WaitForApprovalOption
{
    public OptionalWaitForApprovalOption() : base()
    {
        Required = false;
    }
}
