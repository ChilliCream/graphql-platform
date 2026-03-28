using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Arguments;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

public sealed class UpdateMockCommand : Command
{
    public UpdateMockCommand()
        : base("update")
    {
        Description = "Updates a mock schema with a new schema and extension file.";

        AddOption(Opt<OptionalExtensionFileOption>.Instance);
        AddOption(Opt<OptionalBaseSchemaFileOption>.Instance);
        AddOption(Opt<OptionalDownstreamUrlOption>.Instance);
        AddOption(Opt<OptionalMockSchemaNameOption>.Instance);
        AddArgument(Opt<OptionalIdArgument>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IMocksClient>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IMocksClient client,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var extensionFile = context.ParseResult.GetValueForOption(Opt<OptionalExtensionFileOption>.Instance);
        var baseSchemaFile = context.ParseResult.GetValueForOption(Opt<OptionalBaseSchemaFileOption>.Instance);
        var downstreamUrl = context.ParseResult.GetValueForOption(Opt<OptionalDownstreamUrlOption>.Instance);
        var mockSchemaName = context.ParseResult.GetValueForOption(Opt<OptionalMockSchemaNameOption>.Instance);
        var mockSchemaId = context.ParseResult.GetValueForArgument(Opt<OptionalIdArgument>.Instance);

        if (string.IsNullOrWhiteSpace(mockSchemaId))
        {
            if (!console.IsHumanReadable())
            {
                throw Exit("The mock schema ID is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(context.BindingContext.GetRequiredService<IApisClient>(), workspaceId)
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

            context.SetResult(MockSchemaDetailPrompt.From(mockSchema).ToObject());
        }
    }
}
