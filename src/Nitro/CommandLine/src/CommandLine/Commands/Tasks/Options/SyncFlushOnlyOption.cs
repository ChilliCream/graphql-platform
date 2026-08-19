namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class SyncFlushOnlyOption : Option<bool>
{
    public SyncFlushOnlyOption() : base("--flush-only")
    {
        Description = "Write the task database to tasks.jsonl";
        Required = false;
    }
}
