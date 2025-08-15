namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Stages;

internal sealed class StageCommand : Command
{
    public StageCommand() : base("stage")
    {
        Description = "Manage stages";

        AddCommand(new EditStagesCommand());
        AddCommand(new ListStagesCommand());
    }
}
