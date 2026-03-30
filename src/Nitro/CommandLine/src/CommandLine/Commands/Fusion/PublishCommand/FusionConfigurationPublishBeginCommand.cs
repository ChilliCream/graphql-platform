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
    public FusionConfigurationPublishBeginCommand(
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IConfigurationService configurationService,
        ISessionService sessionService,
        IFileSystem fileSystem,
        IResultHolder resultHolder) : base("begin")
    {
        Description = "Begin a configuration publish. This command will request a deployment slot";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<OptionalSubgraphIdOption>.Instance);
        Options.Add(Opt<OptionalSubgraphNameOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(
                parseResult,
                console,
                fusionConfigurationClient,
                configurationService,
                sessionService,
                fileSystem,
                resultHolder,
                cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IFusionConfigurationClient fusionConfigurationClient,
        IConfigurationService configurationService,
        ISessionService sessionService,
        IFileSystem fileSystem,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetValue(Opt<StageNameOption>.Instance)!;
        var apiId = parseResult.GetValue(Opt<ApiIdOption>.Instance)!;
        var tag = parseResult.GetValue(Opt<TagOption>.Instance)!;
        var subgraphId =
            parseResult.GetValue(Opt<OptionalSubgraphIdOption>.Instance)!;
        var subgraphName =
            parseResult.GetValue(Opt<OptionalSubgraphNameOption>.Instance)!;
        var waitForApproval =
            parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson =
            parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity($"Requesting deployment slot for stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'"))
        {
            await RequestDeploymentSlotAsync(activity);
        }

        return ExitCodes.Success;

        async Task RequestDeploymentSlotAsync(INitroConsoleActivity activity)
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

            resultHolder.SetResult(new ObjectResult(new FusionConfigurationPublishBeginCommandResult { RequestId = requestId }));

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
