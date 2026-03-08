using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp;

internal sealed class UploadMcpFeatureCollectionCommand : Command
{
    public UploadMcpFeatureCollectionCommand() : base("upload")
    {
        Description = "Upload a new MCP Feature Collection version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<McpFeatureCollectionIdOption>.Instance);
        AddOption(Opt<McpPromptFilePatternOption>.Instance);
        AddOption(Opt<McpToolFilePatternOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<McpPromptFilePatternOption>.Instance,
            Opt<McpToolFilePatternOption>.Instance,
            Opt<McpFeatureCollectionIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        List<string> promptPatterns,
        List<string> toolPatterns,
        string mcpFeatureCollectionId,
        CancellationToken cancellationToken)
    {
        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Uploading new MCP Feature Collection version...", UploadMcpFeatureCollection);
        }
        else
        {
            await UploadMcpFeatureCollection(null);
        }

        return ExitCodes.Success;

        async Task UploadMcpFeatureCollection(StatusContext? ctx)
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

            var promptFiles = GlobMatcher.Match(promptPatterns).ToArray();
            var toolFiles = GlobMatcher.Match(toolPatterns).ToArray();

            if (promptFiles.Length < 1 && toolFiles.Length < 1)
            {
                console.WriteLine("Could not find any MCP prompt or tool definition files with the provided patterns.");
                return;
            }

            console.Log($"Found {promptFiles.Length} MCP prompt definition file(s).");
            console.Log($"Found {toolFiles.Length} MCP tool definition file(s).");

            var archiveStream =
                await McpFeatureCollectionHelpers.BuildMcpFeatureCollectionArchive(
                    promptFiles,
                    toolFiles,
                    cancellationToken);

            var input = new UploadMcpFeatureCollectionInput
            {
                Collection = new Upload(archiveStream, "collection.zip"),
                McpFeatureCollectionId = mcpFeatureCollectionId,
                Tag = tag
            };

            console.Log("Uploading MCP Feature Collection..");
            var result = await client.UploadMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadMcpFeatureCollection.Errors);

            if (data.UploadMcpFeatureCollection.McpFeatureCollectionVersion?.Id is null)
            {
                throw new ExitException("Upload of MCP Feature Collection failed.");
            }

            console.Success("Successfully uploaded new MCP Feature Collection version!");
        }
    }
}
