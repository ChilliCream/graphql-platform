using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class ValidateSchemaCommand : Command
{
    public ValidateSchemaCommand() : base("validate")
    {
        Description = "Validates a schema against a stage";

        Options.Add(Opt<StageNameOption>.Instance);
        Options.Add(Opt<ApiIdOption>.Instance);
        Options.Add(Opt<SchemaFileOption>.Instance);
        Options.Add(Opt<OptionalSourceMetadataOption>.Instance);

        this.SetHandler(async context =>
        {
            var console = context.BindingContext.GetRequiredService<INitroConsole>();
            var client = context.BindingContext.GetRequiredService<ISchemasClient>();
            var fileSystem = context.BindingContext.GetRequiredService<IFileSystem>();
            var stage = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
            var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
            var schemaFilePath = context.ParseResult.GetValueForOption(Opt<SchemaFileOption>.Instance)!;
            var sourceMetadataJson = context.ParseResult.GetValueForOption(Opt<OptionalSourceMetadataOption>.Instance);

            context.ExitCode = await ExecuteAsync(
                console,
                client,
                fileSystem,
                stage,
                apiId,
                schemaFilePath,
                sourceMetadataJson,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        INitroConsole console,
        ISchemasClient client,
        IFileSystem fileSystem,
        string stage,
        string apiId,
        string schemaFilePath,
        string? sourceMetadataJson,
        CancellationToken ct)
    {
        var source = SourceMetadataParser.Parse(sourceMetadataJson);

        await using (var activity = console.StartActivity("Validating..."))
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{schemaFilePath.EscapeMarkup()}[/]");

            await using var stream = fileSystem.OpenReadStream(schemaFilePath);

            console.Log("Create validation request");

            var validationRequest = await client.StartSchemaValidationAsync(
                apiId,
                stage,
                stream,
                source,
                ct);

            console.PrintMutationErrorsAndExit(validationRequest.Errors);
            if (validationRequest.Id is not { } requestId)
            {
                throw new ExitException("Could not create validation request!");
            }

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            await foreach (var update in client.SubscribeToSchemaValidationAsync(requestId, ct))
            {
                switch (update)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                        console.WriteLine("The schema is invalid:");
                        console.PrintMutationErrors(schemaErrors);
                        return ExitCodes.Error;

                    case ISchemaVersionValidationSuccess:
                        console.Success("Schema validation succeeded.");
                        return ExitCodes.Success;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        activity.Update("The validation is in progress.");
                        break;

                    default:
                        activity.Update(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }

        return ExitCodes.Error;
    }
}
