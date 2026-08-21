namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskDependencyTypeOption : Option<string>
{
    public TaskDependencyTypeOption() : base("--type")
    {
        Description = "The dependency type (blocks, parent-child, waits-for, related, "
            + "...; default blocks)";
        Required = false;
    }
}
