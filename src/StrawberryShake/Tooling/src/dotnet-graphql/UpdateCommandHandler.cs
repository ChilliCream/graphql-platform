using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using StrawberryShake.Tools.Config;
using StrawberryShake.Tools.OAuth;
using Newtonsoft.Json;
using System.Text;

namespace StrawberryShake.Tools
{
    public class UpdateCommandHandler
        : CommandHandler<UpdateCommandArguments>
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
            string clientDirectory,
            CancellationToken cancellationToken)
        {
            string configFilePath = Path.Combine(clientDirectory, WellKnownFiles.Config);
            var buffer = await FileSystem.ReadAllBytesAsync(configFilePath).ConfigureAwait(false);
            var json = Encoding.UTF8.GetString(buffer);
            GraphQLConfig configuration = JsonConvert.DeserializeObject<GraphQLConfig>(json);

            if (configuration is not null &&
                await UpdateSchemaAsync(context, clientDirectory, configuration, cancellationToken)
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
            var noErrors = true;

            if (configuration.Extensions.StrawberryShake.Url is not null)
            {
                var uri = new Uri(configuration.Extensions.StrawberryShake.Url);
                var schemaFilePath = Path.Combine(clientDirectory, configuration.Schema);

                if (!await DownloadSchemaAsync(context, uri, schemaFilePath, cancellationToken)
                    .ConfigureAwait(false))
                {
                    noErrors = false;
                }
            }

            return !hasErrors;
        }

        private async Task<bool> DownloadSchemaAsync(
            UpdateCommandContext context,
            Uri serviceUri,
            string schemaFilePath,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Download schema");

            HttpClient client = HttpClientFactory.Create(
                context.Uri ?? serviceUri,
                context.Token,
                context.Scheme);

            return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
