using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Mocks;

internal sealed class CreateMockCommand : Command
{
    public CreateMockCommand() : base("create")
    {
        Description = "Create a new mock schema.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<ExtensionFileOption>.Instance);
        Options.Add(Opt<BaseSchemaFileOption>.Instance);
        Options.Add(Opt<DownstreamUrlOption>.Instance);
        Options.Add(Opt<MockSchemaNameOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            mock create \
              --schema "./schema.graphqls" \
              --url "https://example.com/graphql" \
              --extension "./extension.graphql" \
              --name "my-mock" \
              --api-id "<api-id>"
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var apisClient = services.GetRequiredService<IApisClient>();
        var client = services.GetRequiredService<IMocksClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var extensionFile =
            parseResult.GetRequiredValue(Opt<ExtensionFileOption>.Instance);
        var baseSchemaFile =
            parseResult.GetRequiredValue(Opt<BaseSchemaFileOption>.Instance);
        var downstreamUrl =
            parseResult.GetRequiredValue(Opt<DownstreamUrlOption>.Instance);
        var mockSchemaName =
            parseResult.GetRequiredValue(Opt<MockSchemaNameOption>.Instance);

        var apiId = await console.GetOrPromptForApiIdAsync(
            "For which API do you want to create a mock schema?",
            parseResult,
            apisClient,
            sessionService,
            cancellationToken);

        await using (var activity = console.StartActivity(
            $"Creating mock schema '{mockSchemaName.EscapeMarkup()}' for API '{apiId.EscapeMarkup()}'",
            "Failed to create the mock schema."))
        {
            await using var extensionFileStream = fileSystem.OpenReadStream(extensionFile);
            await using var schemaFileStream = fileSystem.OpenReadStream(baseSchemaFile);

            var createdMock = await client.CreateMockSchemaAsync(
                apiId,
                schemaFileStream,
                downstreamUrl,
                extensionFileStream,
                mockSchemaName,
                cancellationToken);

            if (createdMock.Errors?.Count > 0)
            {
                activity.Fail();

                foreach (var error in createdMock.Errors)
                {
                    var errorMessage = error switch
                    {
                        IApiNotFoundError err => err.Message,
                        IMockSchemaNonUniqueNameError err => err.Message,
                        IUnauthorizedOperation err => err.Message,
                        IValidationError err => err.Message,
                        IError err => ErrorMessages.UnexpectedMutationError(err),
                        _ => ErrorMessages.UnexpectedMutationError()
                    };

                    console.Error.WriteErrorLine(errorMessage);
                }

                return ExitCodes.Error;
            }

            if (createdMock.MockSchema is not IMockSchemaDetailPrompt mockSchema)
            {
                throw MutationReturnedNoData();
            }

            activity.Success($"Created mock schema '{mockSchemaName.EscapeMarkup()}'.");

            resultHolder.SetResult(new ObjectResult(MockSchemaDetailPrompt.From(mockSchema).ToObject()));

            return ExitCodes.Success;
        }
    }
}
