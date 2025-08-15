using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishStartCommand : Command
{
    public FusionConfigurationPublishStartCommand() : base("start")
    {
        Description = "Start a fusion configuration publish.";
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

        var input = new StartFusionConfigurationCompositionInput() { RequestId = requestId };

        var result =
            await client.StartFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);
        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.StartFusionConfigurationComposition.Errors);

        console.MarkupLine("Started composition of fusion configuration.");

        return ExitCodes.Success;
    }
}
