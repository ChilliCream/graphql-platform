namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskClaimOption : Option<bool>
{
    public TaskClaimOption() : base("--claim")
    {
        Description = "Shorthand for --status in_progress --assignee <actor>";
        Required = false;
    }
}
