using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;

namespace ChilliCream.Nitro.CLI.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishCancelCommand : Command
{
    public FusionConfigurationPublishCancelCommand() : base("cancel")
    {
        Description = "Cancels a fusion configuration publish.";
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

        console.Title("Cancel the composition of a fusion configuration");

        var input = new CancelFusionConfigurationCompositionInput { RequestId = requestId };

        var result =
            await client.CancelFusionConfigurationPublish.ExecuteAsync(input, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.CancelFusionConfigurationComposition.Errors);

        console.MarkupLine("Cancelled the composition of fusion configuration.");

        return ExitCodes.Success;
    }
}
