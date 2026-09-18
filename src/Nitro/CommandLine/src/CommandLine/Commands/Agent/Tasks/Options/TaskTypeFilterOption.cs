namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskTypeFilterOption : Option<string>
{
    public TaskTypeFilterOption() : base("--type")
    {
        Description =
            "Only show tasks of this type (task, bug, feature, epic, chore, docs, question, or custom)";
    }
}
