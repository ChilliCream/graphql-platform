namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionCommand : Command
{
    public FusionCommand() : base("fusion")
    {
        Description = "Manage Fusion configurations";

        AddCommand(new FusionComposeCommand());
        AddCommand(new FusionDownloadCommand());
        AddCommand(new FusionPublishCommand());
        AddCommand(new FusionRunCommand());
        AddCommand(new FusionSettingsCommand());
        AddCommand(new FusionValidateCommand());
    }
}
