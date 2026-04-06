using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand() : base("validate")
    {
        Description = "Validate a schema against a stage.";

        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples(
            """
            schema validate \
              --api-id "<api-id>" \
              --stage "dev" \
              --schema-file ./schema.graphqls
            """);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ISchemasClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var stage = parseResult.GetRequiredValue(Opt<StageNameOption>.Instance);
        var apiId = parseResult.GetRequiredValue(Opt<ApiIdOption>.Instance);
        var schemaFilePath = parseResult.GetRequiredValue(Opt<SchemaFileOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (!Path.IsPathRooted(schemaFilePath))
        {
            schemaFilePath = Path.Combine(fileSystem.GetCurrentDirectory(), schemaFilePath);
        }

        if (!fileSystem.FileExists(schemaFilePath))
        {
            throw new ExitException(ErrorMessages.SchemaFileDoesNotExist(schemaFilePath));
        }

        await using var activity = console.StartActivity(
            $"Validating schema against stage '{stage.EscapeMarkup()}' of API '{apiId.EscapeMarkup()}'",
            "Failed to validate the schema.");

        await using var stream = fileSystem.OpenReadStream(schemaFilePath);

        var isValid = await SchemaHelpers.ValidateSchemaAsync(
            activity,
            console,
            client,
            apiId,
            stage,
            stream,
            source,
            ct);

        return isValid ? ExitCodes.Success : ExitCodes.Error;
    }
}
