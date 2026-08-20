namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskAcceptanceCriteriaOption : Option<string>
{
    public TaskAcceptanceCriteriaOption() : base("--acceptance-criteria")
    {
        Description = "The acceptance criteria";
        Required = false;
    }
}
