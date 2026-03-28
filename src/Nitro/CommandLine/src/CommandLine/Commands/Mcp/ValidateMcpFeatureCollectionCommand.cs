using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class ValidateMcpFeatureCollectionCommand : Command
{
    public ValidateMcpFeatureCollectionCommand() : base("validate")
    {
        Description = "Validate an MCP Feature Collection version";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<McpFeatureCollectionIdOption>.Instance);
        AddOption(Opt<McpPromptFilePatternOption>.Instance);
        AddOption(Opt<McpToolFilePatternOption>.Instance);
        AddOption(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<IAnsiConsole>();
            var client = context.BindingContext.GetRequiredService<IMcpClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var stage = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var mcpFeatureCollectionId = context.ParseResult.GetValueForOption(Opt<McpFeatureCollectionIdOption>.Instance)!;
            var promptPatterns = context.ParseResult.GetValueForOption(Opt<McpPromptFilePatternOption>.Instance)!;
            var toolPatterns = context.ParseResult.GetValueForOption(Opt<McpToolFilePatternOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                stage,
                mcpFeatureCollectionId,
                promptPatterns,
                toolPatterns,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IMcpClient client,
        IFileSystem fileSystem,
        string stage,
        string mcpFeatureCollectionId,
        List<string> promptPatterns,
        List<string> toolPatterns,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating..."))
        {
            console.Log("Searching for MCP prompt definition files with the following patterns:");
            foreach (var promptPattern in promptPatterns)
            {
                console.Log($"- {promptPattern}");
            }

            console.Log("Searching for MCP tool definition files with the following patterns:");
            foreach (var toolPattern in toolPatterns)
            {
                console.Log($"- {toolPattern}");
            }

            var promptFiles = fileSystem.GlobMatch(promptPatterns, ["**/bin/**", "**/obj/**"]).ToArray();
            var toolFiles = fileSystem.GlobMatch(toolPatterns, ["**/bin/**", "**/obj/**"]).ToArray();

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                console.WriteLine("Could not find any MCP prompt or tool definition files with the provided patterns.");
                return ExitCodes.Error;
            }

            console.Log($"Found {promptFiles.Length} MCP prompt definition file(s).");
            console.Log($"Found {toolFiles.Length} MCP tool definition file(s).");

            var archiveStream =
                await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(
                    fileSystem,
                    promptFiles,
                    toolFiles,
                    ct);

            var validationRequest = await client.StartMcpFeatureCollectionValidationAsync(
                mcpFeatureCollectionId,
                stage,
                archiveStream,
                source,
                ct);

            console.PrintMutationErrorsAndExit(validationRequest.Errors);
            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToMcpFeatureCollectionValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case IMcpFeatureCollectionVersionValidationFailed { Errors: var errors }:
                        console.ErrorLine("The MCP Feature Collection is invalid:");
                        console.PrintMutationErrors(errors);
                        return ExitCodes.Error;

                    case IMcpFeatureCollectionVersionValidationSuccess:
                        console.Success("MCP Feature Collection validation succeeded");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }

        return ExitCodes.Error;
    }
}
