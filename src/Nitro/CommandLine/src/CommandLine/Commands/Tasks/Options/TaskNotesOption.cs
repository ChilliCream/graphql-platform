namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskNotesOption : Option<string>
{
    public TaskNotesOption() : base("--notes")
    {
        Description = "The task notes";
        Required = false;
    }
}
