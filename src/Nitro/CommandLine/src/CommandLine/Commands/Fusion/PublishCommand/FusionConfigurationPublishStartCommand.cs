using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.Client.FusionConfiguration;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishStartCommand : Command
{
    public FusionConfigurationPublishStartCommand() : base("start")
    {
        Description = "Start a Fusion configuration publish.";
        Options.Add(Opt<OptionalRequestIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IFusionConfigurationClient>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(fileSystem, cancellationToken) ??
            throw new ExitException(
                "No request ID was provided and no request ID was found in the cache. Please provide a request ID.");

        await fusionConfigurationClient.ClaimDeploymentSlotAsync(requestId, cancellationToken);

        console.MarkupLine("Started composition of Fusion configuration.");

        return ExitCodes.Success;
    }
}
