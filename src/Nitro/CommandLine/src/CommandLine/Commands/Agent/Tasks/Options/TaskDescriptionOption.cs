namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskDescriptionOption : Option<string>
{
    public TaskDescriptionOption() : base("--description")
    {
        Description = "The task description";
        Required = false;
    }
}
