using StrawberryShake.Tools.Configuration;
using static StrawberryShake.Tools.Configuration.FileContents;

namespace StrawberryShake.Tools;

public class InitCommandHandler : CommandHandler<InitCommandArguments>
{
    public InitCommandHandler(
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
        InitCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        using var command = Output.WriteCommand();

        var accessToken =
            await arguments.AuthArguments
                .RequestTokenAsync(Output, cancellationToken)
                .ConfigureAwait(false);

        var context = new InitCommandContext(
            arguments.Name.Value()?.Trim() ??
            Path.GetFileName(Environment.CurrentDirectory),
            FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
            arguments.Uri.Value!,
            accessToken?.Token,
            accessToken?.Scheme,
            CustomHeaderHelper.ParseHeadersArgument(arguments.CustomHeaders.Values),
            arguments.TypeDepth.HasValue() &&
            int.TryParse(arguments.TypeDepth.Value(), out var typeDepth) &&
            typeDepth >= 3 ? typeDepth : 6);

        if (arguments.FromFile.HasValue())
        {
            using var activity = Output.WriteActivity("Copy schema");

            var schemaFilePath = FileSystem.CombinePath(
                context.Path, context.SchemaFileName);
            var schemaExtensionFilePath = FileSystem.CombinePath(
                context.Path, context.SchemaExtensionFileName);

            File.Copy(context.Uri, schemaFilePath);

            await FileSystem.WriteTextAsync(
                    schemaExtensionFilePath,
                    SchemaExtensionFileContent)
                .ConfigureAwait(false);

            await WriteConfigurationAsync(context);

            return 0;
        }

        if (await ExecuteInternalAsync(context, cancellationToken).ConfigureAwait(false))
        {
            return 0;
        }

        return 1;
    }

    private async Task<bool> ExecuteInternalAsync(
        InitCommandContext context,
        CancellationToken cancellationToken)
    {
        FileSystem.EnsureDirectoryExists(context.Path);

        if (await DownloadSchemaAsync(context, cancellationToken).ConfigureAwait(false))
        {
            await WriteConfigurationAsync(context).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private async Task<bool> DownloadSchemaAsync(
        InitCommandContext context,
        CancellationToken cancellationToken)
    {
        using var activity = Output.WriteActivity("Download schema");

        var schemaFilePath = FileSystem.CombinePath(
            context.Path, context.SchemaFileName);
        var schemaExtensionFilePath = FileSystem.CombinePath(
            context.Path, context.SchemaExtensionFileName);

        var client = HttpClientFactory.Create(
            new(context.Uri), context.Token, context.Scheme, context.CustomHeaders);

        if (await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath, context.TypeDepth,
                cancellationToken)
            .ConfigureAwait(false))
        {
            await FileSystem.WriteTextAsync(
                    schemaExtensionFilePath,
                    SchemaExtensionFileContent)
                .ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private async Task WriteConfigurationAsync(
        InitCommandContext context)
    {
        using var activity = Output.WriteActivity("Client configuration");

        var configFilePath = FileSystem.CombinePath(
            context.Path, context.ConfigFileName);

        var configuration = new GraphQLConfig
        {
            Schema = context.SchemaFileName,
            Extensions =
            {
                StrawberryShake =
                {
                    Name = context.ClientName,
                    Namespace = context.CustomNamespace,
                    Url = context.Uri,
                },
            },
        };

        await FileSystem.WriteTextAsync(configFilePath, configuration.ToString()).ConfigureAwait(false);
    }
}
