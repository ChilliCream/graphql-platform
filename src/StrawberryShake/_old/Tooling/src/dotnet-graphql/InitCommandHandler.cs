using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandHandler
        : CommandHandler<InitCommandArguments>
    {
        public InitCommandHandler(
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
            InitCommandArguments arguments,
            CancellationToken cancellationToken = default)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new InitCommandContext(
                arguments.Schema.Value()?.Trim() ?? "schema",
                FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
                new Uri(arguments.Uri.Value!),
                accessToken?.Token,
                accessToken?.Scheme);

            if(await ExecuteInternalAsync(context, cancellationToken).ConfigureAwait(false))
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
                await WriteConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private async Task<bool> DownloadSchemaAsync(
            InitCommandContext context,
            CancellationToken cancellationToken)
        {
            if(context.Uri == null)
            {
                return true;
            }

            using IActivity activity = Output.WriteActivity("Download schema");

            string schemaFilePath = FileSystem.CombinePath(
                context.Path, context.SchemaFileName);

            HttpClient client = HttpClientFactory.Create(
                context.Uri, context.Token, context.Scheme);

            return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WriteConfigurationAsync(
           InitCommandContext context,
           CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Client configuration");

            Configuration configuration = ConfigurationStore.New();

            configuration.ClientName = context.ClientName;
            configuration.Schemas.Add(new SchemaFile
            {
                Type = "http",
                Name = context.SchemaName,
                File = context.SchemaFileName,
                Url = context.Uri.ToString()
            });

            await ConfigurationStore.SaveAsync(context.Path, configuration).ConfigureAwait(false);
        }
    }
}
