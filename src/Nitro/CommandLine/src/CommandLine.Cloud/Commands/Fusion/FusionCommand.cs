namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionCommand : Command
{
    public FusionCommand() : base("fusion")
    {
        Description = "Manage Fusion configurations";

        AddCommand(new FusionDownloadCommand());
        AddCommand(new FusionPublishCommand());
        AddCommand(new FusionSettingsCommand());
        AddCommand(new FusionValidateCommand());
    }
}
