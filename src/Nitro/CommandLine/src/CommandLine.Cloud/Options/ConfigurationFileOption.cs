namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal sealed class ConfigurationFileOption : Option<FileInfo>
{
    public ConfigurationFileOption() : base("--configuration")
    {
        Description = "The path to the fusion configuration file.";
        IsRequired = true;
        this.DefaultFileFromEnvironmentValue("FUSION_CONFIG_FILE");
    }
}
