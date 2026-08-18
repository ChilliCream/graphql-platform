namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskAllOption : Option<bool>
{
    public TaskAllOption() : base("--all")
    {
        Description = "Include closed and tombstoned tasks";
        Required = false;
    }
}
