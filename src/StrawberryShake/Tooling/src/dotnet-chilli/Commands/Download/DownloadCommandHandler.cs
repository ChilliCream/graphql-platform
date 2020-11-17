using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Http;
using StrawberryShake.Tools.OAuth;
using IHttpClientFactory = StrawberryShake.Tools.Abstractions.IHttpClientFactory;

namespace StrawberryShake.Tools.Commands.Download
{
    public class DownloadCommandHandler
    {
        public DownloadCommandHandler(
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

        public async Task<int> ExecuteAsync(Options.Download arguments)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments
                    .RequestTokenAsync(Output, CancellationToken.None)
                    .ConfigureAwait(false);

            var context = new DownloadCommandContext(
                new Uri(arguments.Uri!),
                FileSystem.ResolvePath(arguments.FileName?.Trim(), "schema.graphql"),
                accessToken?.Token,
                accessToken?.Scheme);

            FileSystem.EnsureDirectoryExists(
                FileSystem.GetDirectoryName(context.FileName));

            return await DownloadSchemaAsync(context)
                .ConfigureAwait(false)
                ? 0 : 1;
        }

        private async Task<bool> DownloadSchemaAsync(DownloadCommandContext context)
        {
            using var activity = Output.WriteActivity("Download schema");
            using var client = HttpClientFactory.Create(context.Uri, context.Token, context.Scheme);
            return await IntrospectionHelper.DownloadSchemaAsync(client, FileSystem, activity, context.FileName, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
