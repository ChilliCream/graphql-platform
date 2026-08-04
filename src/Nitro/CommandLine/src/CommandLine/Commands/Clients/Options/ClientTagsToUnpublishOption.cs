namespace ChilliCream.Nitro.CommandLine.Commands.Clients.Options;

internal sealed class ClientTagsToUnpublishOption : Option<IEnumerable<string>>
{
    public ClientTagsToUnpublishOption() : base("--tag")
    {
        Description = "One or more client version tags to unpublish";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Tag);
    }
}
