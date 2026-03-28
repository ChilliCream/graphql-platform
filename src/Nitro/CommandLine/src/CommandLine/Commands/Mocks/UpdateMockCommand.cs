using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class UpdateMockCommand : Command
{
    public UpdateMockCommand(
        INitroConsole console,
        IApisClient apisClient,
        IMocksClient client,
        IFileSystem fileSystem,
        ISessionService sessionService,
        IResultHolder resultHolder)
        : base("update")
    {
        Description = "Updates a mock schema with a new schema and extension file.";

        Options.Add(Opt<OptionalExtensionFileOption>.Instance);
        Options.Add(Opt<OptionalBaseSchemaFileOption>.Instance);
        Options.Add(Opt<OptionalDownstreamUrlOption>.Instance);
        Options.Add(Opt<OptionalMockSchemaNameOption>.Instance);
        Arguments.Add(Opt<OptionalIdArgument>.Instance);

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
        var extensionFile = parseResult.GetValue(Opt<OptionalExtensionFileOption>.Instance);
        var baseSchemaFile = parseResult.GetValue(Opt<OptionalBaseSchemaFileOption>.Instance);
        var downstreamUrl = parseResult.GetValue(Opt<OptionalDownstreamUrlOption>.Instance);
        var mockSchemaName = parseResult.GetValue(Opt<OptionalMockSchemaNameOption>.Instance);
        var mockSchemaId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (string.IsNullOrWhiteSpace(mockSchemaId))
        {
            if (!console.IsInteractive)
            {
                throw Exit("The mock schema ID is required in non-interactive mode.");
            }

            var workspaceId = parseResult.GetWorkspaceId(sessionService);

            var selectedApi = await SelectApiPrompt
                .New(apisClient, workspaceId)
                .RenderAsync(console, cancellationToken);

            if (selectedApi?.Id is null)
            {
                throw new ExitException("No API selected.");
            }

            var selectedMock = await SelectMockSchemaPrompt
                .New(client, selectedApi.Id)
                .RenderAsync(console, cancellationToken);

            mockSchemaId = selectedMock?.Id ?? throw new ExitException("No mock schema selected.");
        }

        await using (var _ = console.StartActivity("Create and initialize new mock..."))
        {
            await CreateNewMock();
        }

        return ExitCodes.Success;

        async Task CreateNewMock()
        {
            console.Log("Creating mock...");

            await using var baseSchemaStream = baseSchemaFile is null
                ? null
                : fileSystem.OpenReadStream(baseSchemaFile);
            await using var extensionStream = extensionFile is null
                ? null
                : fileSystem.OpenReadStream(extensionFile);

            var updatedMock = await client.UpdateMockSchemaAsync(
                mockSchemaId,
                baseSchemaStream,
                downstreamUrl,
                extensionStream,
                mockSchemaName,
                cancellationToken);
            console.PrintMutationErrorsAndExit(updatedMock.Errors);

            if (updatedMock.MockSchema is not IMockSchemaDetailPrompt mockSchema)
            {
                throw new ExitException("Could not update mock schema.");
            }

            console.Log("Mock schema created.");

            resultHolder.SetResult(new ObjectResult(MockSchemaDetailPrompt.From(mockSchema).ToObject()));
        }
    }
}
