using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionSettingsSetCommand : Command
{
    public FusionSettingsSetCommand() : base("set")
    {
        Description = "Sets a Fusion composition setting and publishes the updated Fusion configuration to Nitro";

        var settingNameArgument = new Argument<string>("SETTING_NAME")
            .FromAmong(SettingNames.GlobalObjectIdentification);

        var settingValueArgument = new Argument<string>("SETTING_VALUE");

        AddArgument(settingNameArgument);
        AddArgument(settingValueArgument);

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<CloudUrlOption>.Instance);
        AddOption(Opt<ApiKeyOption>.Instance);

        this.SetHandler(async context =>
        {
            var settingName = context.ParseResult.GetValueForArgument(settingNameArgument);
            var settingValue = context.ParseResult.GetValueForArgument(settingValueArgument);

            var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var apiClient = context.BindingContext.GetRequiredService<IApiClient>();
            var httpClientFactory = context.BindingContext.GetRequiredService<IHttpClientFactory>();

            context.ExitCode = await ExecuteAsync(
                settingName,
                settingValue,
                apiId,
                stageName,
                tag,
                console,
                apiClient,
                httpClientFactory,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        string settingName,
        string settingValue,
        string apiId,
        string stageName,
        string tag,
        IAnsiConsole console,
        IApiClient client,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        switch (settingName)
        {
            case SettingNames.GlobalObjectIdentification:
                if (!bool.TryParse(settingValue, out var enableGlobalObjectIdentification))
                {
                    console.ErrorLine($"Expected a boolean value for setting '{settingName}'.");
                    return CommandLine.ExitCodes.Error;
                }

                return await FusionPublishCommand.ExecuteAsync(
                    null,
                    [],
                    apiId,
                    stageName,
                    tag,
                    enableGlobalObjectIdentification,
                    requireExistingConfiguration: true,
                    console,
                    client,
                    httpClientFactory,
                    cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(settingName));
        }
    }

    private static class SettingNames
    {
        public const string GlobalObjectIdentification = "global-object-identification";
    }
}
