namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskEstimateOption : Option<int?>
{
    public TaskEstimateOption() : base("--estimate")
    {
        Description = "The estimated effort in minutes";
        Required = false;
    }
}
