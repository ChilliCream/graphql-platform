namespace ChilliCream.Nitro.CLI.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishCommand : Command
{
    public FusionConfigurationPublishCommand() : base("publish")
    {
        Description = "Publish a configuration";

        AddCommand(new FusionConfigurationPublishBeginCommand());
        AddCommand(new FusionConfigurationPublishStartCommand());
        AddCommand(new FusionConfigurationPublishValidateCommand());
        AddCommand(new FusionConfigurationPublishCancelCommand());
        AddCommand(new FusionConfigurationPublishCommitCommand());
    }
}
