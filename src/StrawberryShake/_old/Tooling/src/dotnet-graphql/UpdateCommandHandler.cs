using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class UpdateCommandHandler
        : CommandHandler<UpdateCommandArguments>
    {
        public UpdateCommandHandler(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IConfigurationStore configurationStore,
            IConsoleOutput output)
        {
            FileSystem = fileSystem;
            HttpClientFactory = httpClientFactory;
            ConfigurationStore = configurationStore;
            Output = output;
        }

        public IFileSystem FileSystem { get; }

        public IHttpClientFactory HttpClientFactory { get; }

        public IConfigurationStore ConfigurationStore { get; }

        public IConsoleOutput Output { get; }

        public override async Task<int> ExecuteAsync(
            UpdateCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new UpdateCommandContext(
                arguments.Uri.HasValue() ? new Uri(arguments.Uri.Value()!.Trim()) : null,
                FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
                accessToken?.Token,
                accessToken?.Scheme);

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
            foreach (string path in FileSystem.GetClientDirectories(FileSystem.CurrentDirectory))
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
            string path,
            CancellationToken cancellationToken)
        {
            Configuration? configuration =
                await ConfigurationStore.TryLoadAsync(path)
                    .ConfigureAwait(false);

            if (configuration is { }
                && await UpdateSchemaAsync(
                    context, path, configuration, cancellationToken))
            {
                return 0;
            }

            return 1;
        }

        private async Task<bool> UpdateSchemaAsync(
            UpdateCommandContext context,
            string path,
            Configuration configuration,
            CancellationToken cancellationToken)
        {
            bool hasErrors = false;

            foreach (SchemaFile schema in configuration.Schemas!)
            {
                if (schema.Type == "http")
                {
                    if (!await DownloadSchemaAsync(
                        context, path, schema, cancellationToken))
                    {
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;
        }

        private async Task<bool> DownloadSchemaAsync(
            UpdateCommandContext context,
            string path,
            SchemaFile schemaFile,
            CancellationToken cancellationToken)
        {
            using var activity = Output.WriteActivity("Download schema");

            HttpClient client = HttpClientFactory.Create(
                    context.Uri ?? new Uri(schemaFile.Url),
                    context.Token,
                    context.Scheme);

            return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFile.File,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
