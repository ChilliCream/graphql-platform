using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishValidateCommand : Command
{
    public FusionConfigurationPublishValidateCommand() : base("validate")
    {
        Description = "Validate a Fusion configuration against the schema and clients.";

        Options.Add(Opt<OptionalRequestIdOption>.Instance);
        Options.Add(Opt<FusionArchiveFileOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("fusion publish validate --archive ./gateway.far");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(Messages.NoFusionRequestId);

        var archiveFile =
            parseResult.GetRequiredValue(Opt<FusionArchiveFileOption>.Instance);

        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw new ExitException(Messages.ArchiveFileDoesNotExist(archiveFile));
        }

        await using (var activity = console.StartActivity(
            "Validating Fusion configuration",
            "Failed to validate the Fusion configuration."))
        {
            await using var stream = fileSystem.OpenReadStream(archiveFile);

            var isValidArchive = await FusionPublishHelpers.ValidateFusionConfigurationAsync(
                requestId,
                stream,
                activity,
                console,
                fusionConfigurationClient,
                cancellationToken);

            if (!isValidArchive)
            {
                throw new ExitException("Fusion configuration failed validation.");
            }

            activity.Success("Fusion configuration passed validation.");

            return ExitCodes.Success;
        }
    }
}
