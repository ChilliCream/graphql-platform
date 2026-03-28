using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Inputs;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

public sealed class CreateMockCommand : Command
{
    public CreateMockCommand()
        : base("create")
    {
        Description = "Create a new mock schema.";

        AddOption(Opt<OptionalApiIdOption>.Instance);
        AddOption(Opt<ExtensionFileOption>.Instance);
        AddOption(Opt<BaseSchemaFileOption>.Instance);
        AddOption(Opt<DownstreamUrlOption>.Instance);
        AddOption(Opt<MockSchemaNameOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IMocksClient>(),
            Bind.FromServiceProvider<IFileSystem>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IMocksClient client,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var extensionFile =
            context.ParseResult.GetValueForOption(Opt<ExtensionFileOption>.Instance)!;
        var baseSchemaFile =
            context.ParseResult.GetValueForOption(Opt<BaseSchemaFileOption>.Instance)!;
        var downstreamUrl =
            context.ParseResult.GetValueForOption(Opt<DownstreamUrlOption>.Instance)!;
        var mockSchemaName =
            context.ParseResult.GetValueForOption(Opt<MockSchemaNameOption>.Instance)!;

        const string apiMessage = "For which API do you want to create a mock schema?";
        var apiId = await context.GetOrPromptForApiIdAsync(apiMessage);

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

            context.SetResult(MockSchemaDetailPrompt.From(mockSchema).ToObject());
        }
    }
}
