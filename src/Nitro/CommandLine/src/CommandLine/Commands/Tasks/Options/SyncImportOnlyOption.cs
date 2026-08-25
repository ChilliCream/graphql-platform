namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class SyncImportOnlyOption : Option<bool>
{
    public SyncImportOnlyOption() : base("--import-only")
    {
        Description = "Load tasks.jsonl into the task database";
        Required = false;
    }
}
