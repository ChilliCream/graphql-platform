namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionSettingsCommand : Command
{
    public FusionSettingsCommand() : base("settings")
    {
        Description = "Manage Fusion settings";

        AddCommand(new FusionSettingsSetCommand());
    }
}
