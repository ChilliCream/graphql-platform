namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskPriorityOption : Option<string>
{
    public TaskPriorityOption() : base("--priority")
    {
        Description = "The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); "
            + "list/ready also accept a range like 0-1 or p0-p1";
        Required = false;
    }
}
