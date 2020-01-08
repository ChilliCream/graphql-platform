using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using StrawberryShake.Tools.OAuth;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public class DownloadCommandHandler
        : CommandHandler<DownloadCommandArguments>
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

        public override async Task<int> ExecuteAsync(
            DownloadCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new DownloadCommandContext(
                new Uri(arguments.Uri.Value!),
                FileSystem.ResolvePath(arguments.FileName.Value()?.Trim(), "schema.graphql"),
                accessToken?.Token,
                accessToken?.Scheme);

            FileSystem.EnsureDirectoryExists(
                FileSystem.GetDirectoryName(context.FileName));

            return await DownloadSchemaAsync(context).ConfigureAwait(false) ? 0 : 1;
        }

        private async Task<bool> DownloadSchemaAsync(DownloadCommandContext context)
        {
            using var activity = Output.WriteActivity("Download schema");

            try
            {
                HttpClient client = HttpClientFactory.Create(
                    context.Uri, context.Token, context.Scheme);
                DocumentNode schema =
                    await IntrospectionClient.LoadSchemaAsync(client)
                        .ConfigureAwait(false);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                await FileSystem.WriteToAsync(context.FileName, stream =>
                    Task.Run(() => SchemaSyntaxSerializer.Serialize(
                        schema, stream, true)))
                        .ConfigureAwait(false);
                return true;
            }
            catch (HttpRequestException ex)
            {
                activity.WriteError(
                    HCErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetCode("HTTP_ERROR")
                        .Build());
                return false;
            }
        }
    }
}
