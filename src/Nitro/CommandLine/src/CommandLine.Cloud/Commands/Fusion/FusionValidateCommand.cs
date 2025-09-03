using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Fusion.Compatibility;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionValidateCommand : Command
{
    public FusionValidateCommand() : base("validate")
    {
        Description = "Validates a fusion configuration against a stage";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<ConfigurationFileOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<StageNameOption>.Instance,
            Opt<ApiIdOption>.Instance,
            Opt<ConfigurationFileOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string stage,
        string apiId,
        FileInfo configFile,
        CancellationToken ct)
    {
        console.Title($"Validate to {stage.EscapeMarkup()}");

        var isValid = false;

        if (console.IsHumandReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Validating...", ValidateSchema);
        }
        else
        {
            await ValidateSchema(null);
        }

        return isValid ? ExitCodes.Success : ExitCodes.Error;

        async Task ValidateSchema(StatusContext? ctx)
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{configFile.FullName.EscapeMarkup()}[/]");

            var stream = FileHelpers.CreateFileStream(configFile);

            await using var package = FusionGraphPackage.Open(stream, FileAccess.Read);
            var schemaStream = await LoadSchemaFile(package, ct);

            var input = new ValidateSchemaInput
            {
                ApiId = apiId,
                Stage = stage,
                Schema = new Upload(schemaStream, "schema.graphql")
            };

            console.Log("Create validation request");

            var requestId = await ValidateAsync(console, client, input, ct);

            console.Log($"Validation request created [grey](ID: {requestId.EscapeMarkup()})[/]");

            using var stopSignal = new Subject<Unit>();

            var subscription = client.OnSchemaVersionValidationUpdated
                .Watch(requestId, ExecutionStrategy.NetworkOnly)
                .TakeUntil(stopSignal);

            await foreach (var x in subscription.ToAsyncEnumerable().WithCancellation(ct))
            {
                if (x.Errors is { Count: > 0 } errors)
                {
                    console.PrintErrorsAndExit(errors);
                    throw Exit("No request id returned");
                }

                switch (x.Data?.OnSchemaVersionValidationUpdate)
                {
                    case ISchemaVersionValidationFailed { Errors: var schemaErrors }:
                        console.Error("The schema is invalid:");
                        console.PrintErrorsAndExit(schemaErrors);
                        stopSignal.OnNext(Unit.Default);
                        break;

                    case ISchemaVersionValidationSuccess:
                        isValid = true;
                        stopSignal.OnNext(Unit.Default);

                        console.Success("Schema validation succeeded.");
                        break;

                    case IOperationInProgress:
                    case IValidationInProgress:
                        ctx?.Status("The validation is in progress.");
                        break;

                    default:
                        ctx?.Status(
                            "This is an unknown response, upgrade Nitro CLI to the latest version.");
                        break;
                }
            }
        }
    }

    private static async Task<string> ValidateAsync(
        IAnsiConsole console,
        IApiClient client,
        ValidateSchemaInput input,
        CancellationToken ct)
    {
        var result = await client.ValidateSchemaVersion.ExecuteAsync(input, ct);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.ValidateSchema.Errors);

        if (data.ValidateSchema.Id is null)
        {
            throw new ExitException("Could not create validation request!");
        }

        return data.ValidateSchema.Id;
    }

    private static async Task<MemoryStream> LoadSchemaFile(
        FusionGraphPackage package,
        CancellationToken ct)
    {
        var schemaNode = await package.GetSchemaAsync(ct);

        var schemaFileStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(schemaFileStream, leaveOpen: true);
        await streamWriter.WriteAsync(schemaNode.ToString());
        streamWriter.Flush();
        schemaFileStream.Position = 0;

        return schemaFileStream;
    }
}
