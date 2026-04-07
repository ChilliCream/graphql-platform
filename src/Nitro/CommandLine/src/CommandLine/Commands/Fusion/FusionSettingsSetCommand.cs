using System.Diagnostics.CodeAnalysis;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
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

        this.AddExamples(
            """
            fusion settings set global-object-identification "true" \
              --archive ./gateway.far \
              --env "dev"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var settingName = parseResult.GetRequiredValue(Opt<FusionSettingsNameArgument>.Instance);
        var settingValue = parseResult.GetRequiredValue(Opt<FusionSettingsValueArgument>.Instance);
        var archiveFile = parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);
        var environment = parseResult.GetValue(Opt<FusionEnvironmentOption>.Instance);

        var compositionSettings = new CompositionSettings();

        switch (settingName)
        {
            case FusionSettingsNameArgument.CacheControlMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var cacheControlMergeBehavior))
                {
                    throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorNames.All)}");
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
                    throw new ExitException($"Expected a boolean value for setting '{settingName}'.");
                }

                compositionSettings.Merger.EnableGlobalObjectIdentification = enableGlobalObjectIdentification;
                break;

            case FusionSettingsNameArgument.TagMergeBehavior:
                if (!TryParseDirectiveMergeBehavior(settingValue, out var tagMergeBehavior))
                {
                    throw new ExitException(
                        $"Expected one of the following values for setting '{settingName}': "
                        + $"{string.Join(", ", DirectiveMergeBehaviorNames.All)}");
                }

                compositionSettings.Merger.TagMergeBehavior = tagMergeBehavior;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(settingName));
        }

        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw new ExitException(Messages.ArchiveFileDoesNotExist(archiveFile));
        }

        using var archive = FusionArchive.Open(archiveFile, mode: FusionArchiveMode.Update);

        environment ??= environmentVariables.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        await using var composeActivity = console.StartActivity(
            "Composing new configuration",
            "Failed to compose new configuration.");

        var (result, compositionLog) = await FusionPublishHelpers.ComposeAsync(
            archive,
            environment,
            [],
            compositionSettings,
            cancellationToken);

        if (result.IsSuccess)
        {
            composeActivity.Success("Composed new configuration.");

            return ExitCodes.Success;
        }
        else
        {
            await composeActivity.FailAllAsync();

            console.WriteLine();
            console.WriteLine("## Composition log");
            console.WriteLine();

            FusionComposeCommand.WriteCompositionLog(
                compositionLog,
                console.Out,
                false);

            foreach (var error in result.Errors)
            {
                console.Error.WriteErrorLine(error.Message);
            }

            throw new ExitException();
        }
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
}
