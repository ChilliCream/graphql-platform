namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalWaitForApprovalOption : WaitForApprovalOption
{
    public OptionalWaitForApprovalOption() : base()
    {
        Required = false;
    }
}
