namespace ChilliCream.Nitro.CLI.Commands.Stages;

internal sealed class StageCommand : Command
{
    public StageCommand() : base("stage")
    {
        Description = "Manage stages";

        AddCommand(new EditStagesCommand());
        AddCommand(new ListStagesCommand());
    }
}
