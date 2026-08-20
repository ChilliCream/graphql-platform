namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class SyncStatusOption : Option<bool>
{
    public SyncStatusOption() : base("--status")
    {
        Description = "Report whether tasks.jsonl and the task database diverge";
        Required = false;
    }
}
