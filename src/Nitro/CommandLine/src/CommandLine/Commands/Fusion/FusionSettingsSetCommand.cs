using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionSettingsSetCommand : Command
{
    public FusionSettingsSetCommand() : base("set")
    {
        Description = "Sets a Fusion composition setting and publishes the updated Fusion configuration to Nitro";

        var settingNameArgument = new Argument<string>("SETTING_NAME")
            .FromAmong(SettingNames.GlobalObjectIdentification, SettingNames.ExcludeByTag);

        var settingValueArgument = new Argument<string>("SETTING_VALUE");

        AddArgument(settingNameArgument);
        AddArgument(settingValueArgument);

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        this.AddNitroCloudDefaultOptions();

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
        var compositionSettings = new CompositionSettings();

        switch (settingName)
        {
            case SettingNames.ExcludeByTag:
                var tags = settingValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                compositionSettings.Preprocessor.ExcludeByTag = tags.ToHashSet();
                break;

            case SettingNames.GlobalObjectIdentification:
                if (!bool.TryParse(settingValue, out var enableGlobalObjectIdentification))
                {
                    console.ErrorLine($"Expected a boolean value for setting '{settingName}'.");
                    return ExitCodes.Error;
                }

                compositionSettings.Merger.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
                break;

            case SettingNames.IncludeSatisfiabilityPaths:
                if (!bool.TryParse(settingValue, out var includeSatisfiabilityPaths))
                {
                    console.ErrorLine($"Expected a boolean value for setting '{settingName}'.");
                    return ExitCodes.Error;
                }

                compositionSettings.Satisfiability.IncludeSatisfiabilityPaths = includeSatisfiabilityPaths;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(settingName));
        }

        return await FusionPublishCommand.PublishFusionConfigurationAsync(
            apiId,
            stageName,
            tag,
            [],
            compositionSettings,
            console,
            client,
            httpClientFactory,
            cancellationToken);
    }

    private static class SettingNames
    {
        public const string ExcludeByTag = "exclude-by-tag";
        public const string GlobalObjectIdentification = "global-object-identification";
        public const string IncludeSatisfiabilityPaths = "include-satisfiability-paths";
    }
}
