namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskTypeOption : Option<string>
{
    public TaskTypeOption() : base("--type")
    {
        Description = "The task type (task, bug, feature, epic, chore, docs, question, or custom)";
        Required = false;
    }
}
