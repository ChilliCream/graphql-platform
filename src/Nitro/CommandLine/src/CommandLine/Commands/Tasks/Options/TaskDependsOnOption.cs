namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskDependsOnOption : Option<string[]>
{
    public TaskDependsOnOption() : base("--depends-on")
    {
        Description = "A dependency as 'id' or 'type:id'; can be used multiple times";
        Required = false;
    }
}
