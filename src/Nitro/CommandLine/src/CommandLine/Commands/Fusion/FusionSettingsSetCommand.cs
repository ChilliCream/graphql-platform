using System.Diagnostics.CodeAnalysis;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
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
        Description = "Set a Fusion composition setting in a Fusion archive.";

        Arguments.Add(Opt<FusionSettingsNameArgument>.Instance);
        Arguments.Add(Opt<FusionSettingsValueArgument>.Instance);

        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        Options.Add(Opt<FusionEnvironmentOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(async (services, parseResult, cancellationToken) =>
        {
            var console = services.GetRequiredService<INitroConsole>();
            var fileSystem = services.GetRequiredService<IFileSystem>();
            var resultHolder = services.GetRequiredService<IResultHolder>();

            var settingName = parseResult.GetRequiredValue(Opt<FusionSettingsNameArgument>.Instance);
            var settingValue = parseResult.GetRequiredValue(Opt<FusionSettingsValueArgument>.Instance);
            var archiveFile = parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);
            var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);

            return await ExecuteAsync(
                settingName,
                settingValue,
                archiveFile!,
                environment,
                console,
                fileSystem,
                resultHolder,
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        string settingName,
        string settingValue,
        string archiveFile,
        string? environment,
        INitroConsole console,
        IFileSystem fileSystem,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.FileExists(archiveFile))
        {
            console.Error.WriteErrorLine($"File '{archiveFile}' does not exist.");
            return ExitCodes.Error;
        }

        var compositionSettings = new CompositionSettings();

        switch (settingName)
        {
            case FusionSettingsNameArgument.CacheControlMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var cacheControlMergeBehavior))
                {
                    console.Error.WriteErrorLine(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorNames.All)}");
                    return ExitCodes.Error;
                }

                compositionSettings.Merger.CacheControlMergeBehavior = cacheControlMergeBehavior;
                break;

            case FusionSettingsNameArgument.ExcludeByTag:
                var tags = settingValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                compositionSettings.Preprocessor.ExcludeByTag = tags.ToHashSet();
                break;

            case FusionSettingsNameArgument.GlobalObjectIdentification:
                if (!bool.TryParse(settingValue, out var enableGlobalObjectIdentification))
                {
                    console.Error.WriteErrorLine($"Expected a boolean value for setting '{settingName}'.");
                    return ExitCodes.Error;
                }

                compositionSettings.Merger.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
                break;

            case FusionSettingsNameArgument.TagMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var tagMergeBehavior))
                {
                    console.Error.WriteErrorLine(
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

        if (success && !console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new FusionSettingsSetResult
            {
                Setting = settingName,
                Value = settingValue
            }));
        }

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

    public class FusionSettingsSetResult
    {
        public required string Setting { get; init; }

        public required string Value { get; init; }
    }
}
