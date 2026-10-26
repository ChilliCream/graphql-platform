namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskNotesOption : Option<string>
{
    public TaskNotesOption() : base("--notes")
    {
        Description = "The task notes";
        Required = false;
    }
}
