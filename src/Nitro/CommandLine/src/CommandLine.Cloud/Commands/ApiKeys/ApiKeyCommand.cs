namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.ApiKey;

internal sealed class ApiKeyCommand : Command
{
    public ApiKeyCommand() : base("api-key")
    {
        Description = "Manage API keys";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateApiKeyCommand());
        AddCommand(new DeleteApiKeyCommand());
        AddCommand(new ListApiKeyCommand());
    }
}
