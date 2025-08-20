namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.ApiKey;

internal sealed class ApiKeyCommand : Command
{
    public ApiKeyCommand() : base("api-key")
    {
        Description = "Use this command to manage api keys";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateApiKeyCommand());
        AddCommand(new DeleteApiKeyCommand());
        AddCommand(new ListApiKeyCommand());
    }
}
