using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand() : base("validate")
    {
        Description = "Validate a schema against a stage.";

        Options.Add(Opt<OptionalApiIdOption>.Instance);
        Options.Add(Opt<OptionalStageNameOption>.Instance);
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
        var apisClient = services.GetRequiredService<IApisClient>();
        var stagesClient = services.GetRequiredService<IStagesClient>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = await console.GetOrPromptForApiIdAsync(
            "For which API?", parseResult, apisClient, sessionService, ct);

        var stage = await console.GetOrPromptForStageNameAsync(
            "Which stage?",
            parseResult,
            Opt<OptionalStageNameOption>.Instance,
            stagesClient,
            apiId,
            ct);

        var schemaFilePath = parseResult.GetRequiredValue(Opt<SchemaFileOption>.Instance);
        var sourceMetadataJson = parseResult.GetValue(Opt<OptionalSourceMetadataOption>.Instance);

        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        if (!Path.IsPathRooted(schemaFilePath))
        {
            schemaFilePath = Path.Combine(fileSystem.GetCurrentDirectory(), schemaFilePath);
        }

        if (!fileSystem.FileExists(schemaFilePath))
        {
            throw new ExitException(Messages.SchemaFileDoesNotExist(schemaFilePath));
        }

        await using var activity = console.StartActivity(
            $"Validating schema of API '{apiId.EscapeMarkup()}' against stage '{stage.EscapeMarkup()}'",
            "Failed to validate the schema.");

        await using var stream = fileSystem.OpenReadStream(schemaFilePath);

        var validationResult = await SchemaHelpers.ValidateSchemaAsync(
            activity,
            console,
            client,
            apiId,
            stage,
            stream,
            source,
            ct);

        if (validationResult is SchemaValidationResult.Failed failed)
        {
            activity.Fail(failed.Details, "Schema failed validation.");

            throw new ExitException("Schema failed validation.");
        }

        activity.Success("Schema passed validation.");

        return ExitCodes.Success;
    }
}
