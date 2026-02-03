using System.CommandLine.IO;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Settings;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class FusionSettingsSetCommand : Command
{
    public FusionSettingsSetCommand() : base("set")
    {
        Description = "Sets a Fusion composition setting and performs a composition";

        var settingNameArgument = new Argument<string>("SETTING_NAME")
            .FromAmong(SettingNames.GlobalObjectIdentification, SettingNames.ExcludeByTag);

        var settingValueArgument = new Argument<string>("SETTING_VALUE");

        AddArgument(settingNameArgument);
        AddArgument(settingValueArgument);

        AddOption(Opt<FusionArchiveFileOption>.Instance);
        this.AddNitroCloudDefaultOptions();

        this.SetHandler(async context =>
        {
            var settingName = context.ParseResult.GetValueForArgument(settingNameArgument);
            var settingValue = context.ParseResult.GetValueForArgument(settingValueArgument);
            var environment = context.ParseResult.GetValueForOption(Opt<FusionArchiveEnvironmentOption>.Instance);
            var archiveFile = context.ParseResult.GetValueForOption(Opt<FusionArchiveFileOption>.Instance)!;

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            context.ExitCode = await ExecuteAsync(
                settingName,
                settingValue,
                archiveFile,
                environment,
                console,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        string settingName,
        string settingValue,
        string archiveFile,
        string? environment,
        IAnsiConsole console,
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

            // TODO: Why do we do this? this setting shouldn't be saved in the composition settings...
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

        var compositionLog = new CompositionLog();

        using var archive = FusionArchive.Open(archiveFile);

        var result = await FusionComposeCommand.ComposeAsync(
            compositionLog,
            [],
            archive,
            environment,
            compositionSettings,
            cancellationToken);

        FusionComposeCommand.WriteCompositionLog(
            compositionLog,
            new AnsiStreamWriter(Console.Out),
            false);

        if (result.IsFailure)
        {
            foreach (var error in result.Errors)
            {
                console.WriteLine(error.Message);
            }

            return ExitCodes.Error;
        }

        return ExitCodes.Success;
    }

    private sealed class AnsiStreamWriter(TextWriter textWriter) : IStandardStreamWriter
    {
        public void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                textWriter.Write(value);
            }
        }
    }

    private static class SettingNames
    {
        public const string ExcludeByTag = "exclude-by-tag";
        public const string GlobalObjectIdentification = "global-object-identification";
        public const string IncludeSatisfiabilityPaths = "include-satisfiability-paths";
    }
}
