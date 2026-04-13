namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class SetConfigCommand : Command
{
    public SetConfigCommand() : base("set")
    {
        Description = "Set an analytical command default (api, stage, format).";

        Subcommands.Add(new SetConfigApiCommand());
        Subcommands.Add(new SetConfigStageCommand());
        Subcommands.Add(new SetConfigFormatCommand());
    }
}
