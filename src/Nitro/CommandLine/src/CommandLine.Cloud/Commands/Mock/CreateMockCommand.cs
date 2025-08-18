using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock;

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
        var extensionFile =
            context.ParseResult.GetValueForOption(Opt<ExtensionFileOption>.Instance)!;
        var baseSchemaFile =
            context.ParseResult.GetValueForOption(Opt<BaseSchemaFileOption>.Instance)!;
        var downstreamUrl =
            context.ParseResult.GetValueForOption(Opt<DownstreamUrlOption>.Instance)!;
        var mockSchemaName =
            context.ParseResult.GetValueForOption(Opt<MockSchemaNameOption>.Instance)!;

        const string apiMessage = "For which api do you want to create a mock schema?";
        var apiId = await context.GetOrSelectApiId(apiMessage);

        if (console.IsHumandReadable())
        {
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

            var extensionFileStream = FileHelpers.CreateFileStream(extensionFile);
            var schemaFileStream = FileHelpers.CreateFileStream(baseSchemaFile);

            console.Log("Uploading Schema..");
            var result = await client.CreateMockSchema.ExecuteAsync(
                apiId,
                new Upload(schemaFileStream, "schema.graphql"),
                downstreamUrl,
                new Upload(extensionFileStream, "extension.graphql"),
                mockSchemaName,
                cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.CreateMockSchema.Errors);

            if (data.CreateMockSchema.MockSchema?.Id is null)
            {
                throw new ExitException("Creating mock schema failed.");
            }

            console.Success("Successfully uploaded schema!");

            context.SetResult(
                MockSchemaDetailPrompt
                    .From(data.CreateMockSchema.MockSchema)
                    .ToObject());
        }
    }
}
