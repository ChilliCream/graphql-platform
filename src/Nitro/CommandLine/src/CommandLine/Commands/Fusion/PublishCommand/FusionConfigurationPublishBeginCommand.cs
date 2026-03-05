using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishBeginCommand : Command
{
    public FusionConfigurationPublishBeginCommand() : base("begin")
    {
        Description = "Begin a configuration publish. This command will request a deployment slot";
        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<OptionalSubgraphIdOption>.Instance);
        AddOption(Opt<OptionalSubgraphNameOption>.Instance);
        AddOption(Opt<OptionalWaitForApprovalOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IConfigurationService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        IConfigurationService configurationService,
        CancellationToken cancellationToken)
    {
        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
        var tag = context.ParseResult.GetValueForOption(Opt<TagOption>.Instance)!;
        var subgraphId =
            context.ParseResult.GetValueForOption(Opt<OptionalSubgraphIdOption>.Instance)!;
        var subgraphName =
            context.ParseResult.GetValueForOption(Opt<OptionalSubgraphNameOption>.Instance)!;
        var waitForApproval =
            context.ParseResult.GetValueForOption(Opt<OptionalWaitForApprovalOption>.Instance);

        console.Title("Requesting a deployment slot");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Requesting deployment slot ...", RequestDeploymentSlotAsync);
        }
        else
        {
            await RequestDeploymentSlotAsync(null);
        }

        return ExitCodes.Success;

        async Task RequestDeploymentSlotAsync(StatusContext? ctx)
        {
            var requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                apiId,
                stageName,
                tag,
                subgraphId,
                subgraphName,
                waitForApproval,
                ctx,
                console,
                client,
                cancellationToken);

            context.SetResult(new FusionConfigurationPublishBeginCommandResult { RequestId = requestId });
            await FusionConfigurationPublishingState.SetRequestId(requestId, cancellationToken);
        }
    }

    public class FusionConfigurationPublishBeginCommandResult
    {
        public required string RequestId { get; init; }
    }
}
