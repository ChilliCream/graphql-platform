using System.Text;
using StrawberryShake.Tools.Configuration;
using static System.IO.Path;

namespace StrawberryShake.Tools;

public class UpdateCommandHandler : CommandHandler<UpdateCommandArguments>
{
    public UpdateCommandHandler(
        IFileSystem fileSystem,
        IHttpClientFactory httpClientFactory,
        IConsoleOutput output)
    {
        FileSystem = fileSystem;
        HttpClientFactory = httpClientFactory;
        Output = output;
    }

    public IFileSystem FileSystem { get; }

    public IHttpClientFactory HttpClientFactory { get; }

    public IConsoleOutput Output { get; }

    public override async Task<int> ExecuteAsync(
        UpdateCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        using var command = Output.WriteCommand();

        var accessToken =
            await arguments.AuthArguments
                .RequestTokenAsync(Output, cancellationToken)
                .ConfigureAwait(false);

        var context = new UpdateCommandContext(
            arguments.Uri.HasValue() ? new Uri(arguments.Uri.Value()!.Trim()) : null,
            FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
            accessToken?.Token,
            accessToken?.Scheme,
            CustomHeaderHelper.ParseHeadersArgument(arguments.CustomHeaders.Values),
            arguments.TypeDepth.HasValue() &&
            int.TryParse(arguments.TypeDepth.Value(), out var typeDepth) &&
            typeDepth >= 3 ? typeDepth : 6);

        return context.Path is null
            ? await FindAndUpdateSchemasAsync(context, cancellationToken)
                .ConfigureAwait(false)
            : await UpdateSingleSchemaAsync(context, context.Path, cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<int> FindAndUpdateSchemasAsync(
        UpdateCommandContext context,
        CancellationToken cancellationToken)
    {
        foreach (var path in FileSystem.GetClientDirectories(FileSystem.CurrentDirectory))
        {
            try
            {
                await UpdateSingleSchemaAsync(
                        context, path, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                return 1;
            }
        }
        return 0;
    }

    private async Task<int> UpdateSingleSchemaAsync(
        UpdateCommandContext context,
        string clientDirectory,
        CancellationToken cancellationToken)
    {
        var configFilePath = Combine(clientDirectory, WellKnownFiles.Config);
        var buffer = await FileSystem.ReadAllBytesAsync(configFilePath).ConfigureAwait(false);
        var json = Encoding.UTF8.GetString(buffer);
        var configuration = GraphQLConfig.FromJson(json);

        if (await UpdateSchemaAsync(context, clientDirectory, configuration, cancellationToken)
                .ConfigureAwait(false))
        {
            return 0;
        }

        return 1;
    }

    private async Task<bool> UpdateSchemaAsync(
        UpdateCommandContext context,
        string clientDirectory,
        GraphQLConfig configuration,
        CancellationToken cancellationToken)
    {
        var hasErrors = false;

        if (configuration.Extensions.StrawberryShake.Url is not null)
        {
            var uri = new Uri(configuration.Extensions.StrawberryShake.Url);
            var schemaFilePath = Combine(clientDirectory, configuration.Schema);
            var tempFile = CreateTempFileName();

            // we first attempt to download the new schema into a temp file.
            // if that should fail we still have the original schema file and
            // the user can still work.
            if (!await DownloadSchemaAsync(context, uri, tempFile, cancellationToken)
                .ConfigureAwait(false))
            {
                hasErrors = true;
            }
            else
            {
                // if the schema download succeeded we will replace the old schema with the
                // new one.
                if (File.Exists(schemaFilePath))
                {
                    File.Delete(schemaFilePath);
                }

                File.Move(tempFile, schemaFilePath);
            }

            // in any case we will make sure the temp file is removed at the end.
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            // remove the temp directory.
            var tempDirectory = GetDirectoryName(tempFile);
            if (Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory);
                }
                catch (IOException)
                {
                    // ignore error when directory is not empty.
                }
            }
        }

        return !hasErrors;
    }

    private static string CreateTempFileName()
    {
        var pathSegment = Random.Shared.Next(9999).ToString();

        for (var i = 0; i < 100; i++)
        {
            var tempFile = Combine(GetTempPath(), pathSegment, GetRandomFileName());

            if (!File.Exists(tempFile))
            {
                var tempDir = GetDirectoryName(tempFile)!;

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                return tempFile;
            }
        }

        throw new InvalidOperationException("Could not acquire a temp file.");
    }

    private async Task<bool> DownloadSchemaAsync(
        UpdateCommandContext context,
        Uri serviceUri,
        string schemaFilePath,
        CancellationToken cancellationToken)
    {
        using var activity = Output.WriteActivity("Download schema");

        var client = HttpClientFactory.Create(
            context.Uri ?? serviceUri,
            context.Token,
            context.Scheme,
            context.CustomHeaders);

        return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath, context.TypeDepth,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
