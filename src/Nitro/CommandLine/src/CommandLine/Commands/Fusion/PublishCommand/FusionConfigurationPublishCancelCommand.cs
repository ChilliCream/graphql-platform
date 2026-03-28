using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCancelCommand : Command
{
    public FusionConfigurationPublishCancelCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem) : base("cancel")
    {
        Description = "Cancels a Fusion configuration publish.";
        Options.Add(Opt<OptionalRequestIdOption>.Instance);

        SetAction((parseResult, cancellationToken)
            => ExecuteAsync(parseResult, console, fusionConfigurationClient, fileSystem, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var requestId =
            parseResult.GetValue(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        await fusionConfigurationClient.ReleaseDeploymentSlotAsync(requestId, cancellationToken);

        console.MarkupLine("Cancelled the composition of Fusion configuration.");

        return ExitCodes.Success;
    }
}
