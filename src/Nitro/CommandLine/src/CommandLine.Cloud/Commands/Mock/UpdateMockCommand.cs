using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using ChilliCream.Nitro.CommandLine;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock;

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
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var extensionFile = context.ParseResult.GetValueForOption(Opt<OptionalExtensionFileOption>.Instance);
        var baseSchemaFile = context.ParseResult.GetValueForOption(Opt<OptionalBaseSchemaFileOption>.Instance);
        var downstreamUrl = context.ParseResult.GetValueForOption(Opt<OptionalDownstreamUrlOption>.Instance);
        var mockSchemaName = context.ParseResult.GetValueForOption(Opt<OptionalMockSchemaNameOption>.Instance);
        var mockSchemaId = context.ParseResult.GetValueForArgument(Opt<OptionalIdArgument>.Instance);

        if (string.IsNullOrWhiteSpace(mockSchemaId))
        {
            if (!console.IsHumandReadable())
            {
                throw Exit("The mock schema id is required in non-interactive mode.");
            }

            var workspaceId = context.RequireWorkspaceId();

            var selectedApi = await SelectApiPrompt
                .New(client, workspaceId)
                .RenderAsync(console, cancellationToken);

            if (selectedApi?.Id is null)
            {
                throw new ExitException("No API selected.");
            }

            var selectedMock = await SelectMockSchemaPrompt
                .New(client, selectedApi.Id)
                .RenderAsync(console, cancellationToken);

            mockSchemaId = selectedMock?.Id ?? throw new ExitException("No mock schema selected.");

            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Create and initialize new mock...", CreateNewMock);
        }
        else
        {
            await CreateNewMock(null);
        }

        return ExitCodes.Success;

        async Task CreateNewMock(StatusContext? ctx)
        {
            console.Log("Creating mock...");

            var result = await client.UpdateMockSchema.ExecuteAsync(
                mockSchemaId,
                baseSchemaFile is not null
                    ? new Upload(FileHelpers.CreateFileStream(baseSchemaFile), "schema.graphql")
                    : null,
                downstreamUrl is not null
                    ? downstreamUrl
                    : null,
                extensionFile is not null
                    ? new Upload(FileHelpers.CreateFileStream(extensionFile), "extension.graphql")
                    : null,
                mockSchemaName is not null
                    ? mockSchemaName
                    : null,
                cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UpdateMockSchema.Errors);

            if (data.UpdateMockSchema.MockSchema?.Id is null)
            {
                throw new ExitException("Creating mock schema failed.");
            }

            console.Log("Mock schema created.");

            context
                .SetResult(MockSchemaDetailPrompt.From(data.UpdateMockSchema.MockSchema)
                    .ToObject());
        }
    }
}
