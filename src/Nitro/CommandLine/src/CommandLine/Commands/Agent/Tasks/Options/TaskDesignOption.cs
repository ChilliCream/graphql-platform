namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskDesignOption : Option<string>
{
    public TaskDesignOption() : base("--design")
    {
        Description = "The task design";
        Required = false;
    }
}
