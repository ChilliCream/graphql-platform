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
        parseResult.AssertHasAuthentication(sessionService);

        var extensionFile = parseResult.GetValue(Opt<OptionalExtensionFileOption>.Instance);
        var baseSchemaFile = parseResult.GetValue(Opt<OptionalBaseSchemaFileOption>.Instance);
        var downstreamUrl = parseResult.GetValue(Opt<OptionalDownstreamUrlOption>.Instance);
        var mockSchemaName = parseResult.GetValue(Opt<OptionalMockSchemaNameOption>.Instance);
        var mockSchemaId = parseResult.GetValue(Opt<OptionalIdArgument>.Instance);

        if (string.IsNullOrWhiteSpace(mockSchemaId))
        {
            if (!console.IsInteractive)
            {
                throw MissingRequiredOption("id");
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

        await using (var activity = console.StartActivity($"Updating mock schema '{mockSchemaId.EscapeMarkup()}'"))
        {
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

            if (updatedMock.Errors?.Count > 0)
            {
                activity.Fail("Failed to update the mock schema.");

                foreach (var error in updatedMock.Errors)
                {
                    var errorMessage = error switch
                    {
                        IMockSchemaNotFoundError err => err.Message,
                        IMockSchemaNonUniqueNameError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IValidationError err => err.Message,
                        IError err => "Unexpected mutation error: " + err.Message,
                        _ => "Unexpected mutation error."
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (updatedMock.MockSchema is not IMockSchemaDetailPrompt mockSchema)
            {
                activity.Fail("Failed to update the mock schema.");
                throw MutationReturnedNoData();
            }

            activity.Success($"Updated mock schema '{mockSchemaId.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(MockSchemaDetailPrompt.From(mockSchema).ToObject()));

            return ExitCodes.Success;
        }
    }
}
