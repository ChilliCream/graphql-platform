namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskPrefixOption : Option<string>
{
    public TaskPrefixOption() : base("--prefix")
    {
        Description = "The task ID prefix (defaults to the current directory name)";
        Required = false;
    }
}
