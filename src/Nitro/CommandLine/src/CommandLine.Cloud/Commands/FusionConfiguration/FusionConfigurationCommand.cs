namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;

internal sealed class FusionConfigurationCommand : Command
{
    public FusionConfigurationCommand() : base("fusion-configuration")
    {
        Description = "Manage fusion configurations";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new FusionConfigurationPublishCommand());
        AddCommand(new FusionConfigurationDownloadCommand());

        // Validate requires .NET 7.0 because of the fusion library
#if NET7_0_OR_GREATER
        AddCommand(new FusionConfigurationValidateCommand());
#endif
    }
}
