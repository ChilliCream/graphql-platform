using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishStartCommand : Command
{
    public FusionConfigurationPublishStartCommand() : base("start")
    {
        Description = "Start a Fusion configuration publish.";
        AddOption(Opt<OptionalRequestIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken cancellationToken)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(cancellationToken) ??
            throw new ExitException(
                "No request id was provided and no request id was found in the cache. Please provide a request id.");

        console.Title("Start the composition of a fusion configuration");

        await FusionPublishHelpers.ClaimDeploymentSlot(requestId, console, client, cancellationToken);

        console.MarkupLine("Started composition of fusion configuration.");

        return ExitCodes.Success;
    }
}
