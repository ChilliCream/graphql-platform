using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class CreateMockCommand : Command
{
    public CreateMockCommand(
        INitroConsole console,
        IApisClient apisClient,
        IMocksClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder)
        : base("create")
    {
        Description = "Create a new mock schema.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<ExtensionFileOption>.Instance);
        Options.Add(Opt<BaseSchemaFileOption>.Instance);
        Options.Add(Opt<DownstreamUrlOption>.Instance);
        Options.Add(Opt<MockSchemaNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(console, async (parseResult, cancellationToken)
            => await ExecuteAsync(parseResult, console, apisClient, client, fileSystem, sessionService, resultHolder, cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IApisClient apisClient,
        IMocksClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        var extensionFile =
            parseResult.GetValue(Opt<ExtensionFileOption>.Instance)!;
        var baseSchemaFile =
            parseResult.GetValue(Opt<BaseSchemaFileOption>.Instance)!;
        var downstreamUrl =
            parseResult.GetValue(Opt<DownstreamUrlOption>.Instance)!;
        var mockSchemaName =
            parseResult.GetValue(Opt<MockSchemaNameOption>.Instance)!;

        const string apiMessage = "For which API do you want to create a mock schema?";
        var apiId = await console.GetOrPromptForApiIdAsync(apiMessage, parseResult, apisClient, sessionService, cancellationToken);

        await using (var _ = console.StartActivity("Create and initialize new mock..."))
        {
            await CreateNewMock();
        }

        return ExitCodes.Success;

        async Task CreateNewMock()
        {
            console.Log("Creating mock...");

            await using var extensionFileStream = fileSystem.OpenReadStream(extensionFile);
            await using var schemaFileStream = fileSystem.OpenReadStream(baseSchemaFile);

            console.Log("Uploading Schema..");
            var createdMock = await client.CreateMockSchemaAsync(
                apiId,
                schemaFileStream,
                downstreamUrl,
                extensionFileStream,
                mockSchemaName,
                cancellationToken);
            console.PrintMutationErrorsAndExit(createdMock.Errors);

            if (createdMock.MockSchema is not IMockSchemaDetailPrompt mockSchema)
            {
                throw new ExitException("Could not create mock schema.");
            }

            console.Success("Successfully uploaded schema!");

            resultHolder.SetResult(new ObjectResult(MockSchemaDetailPrompt.From(mockSchema).ToObject()));
        }
    }
}
