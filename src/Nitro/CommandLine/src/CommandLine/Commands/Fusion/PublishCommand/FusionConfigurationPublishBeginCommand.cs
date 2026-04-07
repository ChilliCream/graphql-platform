using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishBeginCommand : Command
{
    public FusionConfigurationPublishBeginCommand() : base("begin")
    {
        Description = "Begin a configuration publish. This command will request a deployment slot.";

        Options.Add(Opt<TagOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<OptionalSubgraphIdOption>.Instance);
        Options.Add(Opt<OptionalSubgraphNameOption>.Instance);
        Options.Add(Opt<OptionalWaitForApprovalOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            fusion publish begin \
              --api-id "<api-id>" \
              --tag "v1" \
              --stage "dev"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var fusionConfigurationClient = services.GetRequiredService<IFusionConfigurationClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var stageName = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var tag = parseResult.GetRequiredValue(Opt<TagOption>.Instance);
        var subgraphId =
            parseResult.GetRequiredValue(Opt<OptionalSubgraphIdOption>.Instance);
        var subgraphName =
            parseResult.GetRequiredValue(Opt<OptionalSubgraphNameOption>.Instance);
        var waitForApproval =
            parseResult.GetValue(Opt<OptionalWaitForApprovalOption>.Instance);
        var sourceMetadataJson =
            parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity(
            $"Requesting deployment slot for stage '{stageName.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to request a deployment slot."))
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
