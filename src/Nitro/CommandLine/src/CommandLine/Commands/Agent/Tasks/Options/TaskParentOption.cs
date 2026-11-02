namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskParentOption : Option<string>
{
    public TaskParentOption() : base("--parent")
    {
        Description = "The parent task ID; the new task becomes its child";
        Required = false;
    }
}
