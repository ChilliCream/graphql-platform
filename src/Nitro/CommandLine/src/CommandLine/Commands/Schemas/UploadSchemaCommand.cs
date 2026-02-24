using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using StrawberryShake;
using Command = System.CommandLine.Command;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas;

internal sealed class UploadSchemaCommand : Command
{
    public UploadSchemaCommand()
        : base("upload")
    {
        Description = "Upload a new schema version";

        AddOption(Opt<TagOption>.Instance);
        AddOption(Opt<SchemaFileOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Opt<TagOption>.Instance,
            Opt<SchemaFileOption>.Instance,
            Opt<ApiIdOption>.Instance,
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        IAnsiConsole console,
        IApiClient client,
        string tag,
        FileInfo schemaFile,
        string apiId,
        CancellationToken cancellationToken)
    {
        console.Title($"Upload schema {schemaFile.FullName.EscapeMarkup()}");

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Upload schema...", UploadSchema);
        }
        else
        {
            await UploadSchema(null);
        }

        return ExitCodes.Success;

        async Task UploadSchema(StatusContext? ctx)
        {
            console.Log("Initialized");
            console.Log($"Reading file [blue]{schemaFile.FullName.EscapeMarkup()}[/]");

            var stream = FileHelpers.CreateFileStream(schemaFile);

            var input = new UploadSchemaInput
            {
                Schema = new Upload(stream, "schema.graphqls"),
                ApiId = apiId,
                Tag = tag
            };

            console.Log("Uploading Schema..");
            var result = await client.UploadSchema.ExecuteAsync(input, cancellationToken);

            console.EnsureNoErrors(result);
            var data = console.EnsureData(result);
            console.PrintErrorsAndExit(data.UploadSchema.Errors);

            if (data.UploadSchema.SchemaVersion?.Id is null)
            {
                throw new ExitException("Upload schema failed!");
            }

            console.Success("Successfully uploaded schema!");
        }
    }
}
