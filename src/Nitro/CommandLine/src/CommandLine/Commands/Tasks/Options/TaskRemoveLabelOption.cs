namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskRemoveLabelOption : Option<string[]>
{
    public TaskRemoveLabelOption() : base("--remove-label")
    {
        Description = "A label to remove; can be used multiple times";
        Required = false;
    }
}
