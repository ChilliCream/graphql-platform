using System.Diagnostics.CodeAnalysis;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class FusionSettingsSetCommand : Command
{
    public FusionSettingsSetCommand() : base("set")
    {
        Description = "Sets a Fusion composition setting in a Fusion archive.";

        var settingNameArgument = new Argument<string>("SETTING_NAME")
            .FromAmong(SettingNames.All);

        var settingValueArgument = new Argument<string>("SETTING_VALUE");

        AddArgument(settingNameArgument);
        AddArgument(settingValueArgument);

        var fusionArchiveOption = new FusionArchiveFileOption(true);

        AddOption(fusionArchiveOption);

        AddOption(Opt<FusionEnvironmentOption>.Instance);

        this.SetHandler(async context =>
        {
            var settingName = context.ParseResult.GetValueForArgument(settingNameArgument);
            var settingValue = context.ParseResult.GetValueForArgument(settingValueArgument);
            var archiveFile = context.ParseResult.GetValueForOption(fusionArchiveOption);
            var environment = context.ParseResult.GetValueForOption(Opt<FusionEnvironmentOption>.Instance);

            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

            context.ExitCode = await ExecuteAsync(
                settingName,
                settingValue,
                archiveFile!,
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
        if (!File.Exists(archiveFile))
        {
            console.ErrorLine($"File '{archiveFile}' does not exist.");
            return ExitCodes.Error;
        }

        var compositionSettings = new CompositionSettings();

        switch (settingName)
        {
            case SettingNames.CacheControlMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var cacheControlMergeBehavior))
                {
                    console.ErrorLine(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorNames.All)}");
                    return ExitCodes.Error;
                }

                compositionSettings.Merger.CacheControlMergeBehavior = cacheControlMergeBehavior;
                break;

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

            case SettingNames.TagMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var tagMergeBehavior))
                {
                    console.ErrorLine(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorNames.All)}");
                    return ExitCodes.Error;
                }

                compositionSettings.Merger.TagMergeBehavior = tagMergeBehavior;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(settingName));
        }

        using var archive = FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update);

        environment ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var success = await FusionPublishHelpers.ComposeAsync(
            archive,
            environment,
            [],
            compositionSettings,
            console,
            cancellationToken);

        return success ? ExitCodes.Success : ExitCodes.Error;
    }

    private static bool TryParseDirectiveMergeBehavior(
        string value,
        [NotNullWhen(true)] out DirectiveMergeBehavior? directiveMergeBehavior)
    {
        directiveMergeBehavior = value switch
        {
            DirectiveMergeBehaviorNames.Ignore => DirectiveMergeBehavior.Ignore,
            DirectiveMergeBehaviorNames.Include => DirectiveMergeBehavior.Include,
            DirectiveMergeBehaviorNames.IncludePrivate => DirectiveMergeBehavior.IncludePrivate,
            _ => null
        };

        return directiveMergeBehavior is not null;
    }

    private static class SettingNames
    {
        public const string CacheControlMergeBehavior = "cache-control-merge-behavior";
        public const string ExcludeByTag = "exclude-by-tag";
        public const string GlobalObjectIdentification = "global-object-identification";
        public const string TagMergeBehavior = "tag-merge-behavior";

        public static readonly string[] All =
        [
            CacheControlMergeBehavior,
            ExcludeByTag,
            GlobalObjectIdentification,
            TagMergeBehavior
        ];
    }

    private static class DirectiveMergeBehaviorNames
    {
        public const string Ignore = "ignore";
        public const string Include = "include";
        public const string IncludePrivate = "include-private";

        public static readonly string[] All =
        [
            Ignore,
            Include,
            IncludePrivate
        ];
    }
}
