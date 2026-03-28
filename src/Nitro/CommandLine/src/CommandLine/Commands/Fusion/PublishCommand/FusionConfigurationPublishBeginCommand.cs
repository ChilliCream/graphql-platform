using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.Client.FusionConfiguration;
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
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IFusionConfigurationClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IConfigurationService>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        ISessionService sessionService,
        IConfigurationService configurationService,
        IFileSystem fileSystem,
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
        var sourceMetadataJson =
            context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Requesting deployment slot ..."))
        {
            await RequestDeploymentSlotAsync(activity);
        }

        return ExitCodes.Success;

        async Task RequestDeploymentSlotAsync(ICommandLineActivity activity)
        {
            var requestId = await FusionPublishHelpers.RequestDeploymentSlotAsync(
                apiId,
                stageName,
                tag,
                subgraphId,
                subgraphName,
                sourceSchemaVersions: null,
                waitForApproval,
                source,
                activity,
                console,
                fusionConfigurationClient,
                cancellationToken);

            context.SetResult(new FusionConfigurationPublishBeginCommandResult { RequestId = requestId });
            await FusionConfigurationPublishingState.SetRequestId(
                fileSystem,
                requestId,
                cancellationToken);
        }
    }

    public class FusionConfigurationPublishBeginCommandResult
    {
        public required string RequestId { get; init; }
    }
}
