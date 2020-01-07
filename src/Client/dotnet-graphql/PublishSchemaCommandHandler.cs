using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandHandler
        : CommandHandler<DownloadCommandArguments>
    {
        public PublishSchemaCommandHandler(
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

            var context = new DownloadCommandContext(
                new Uri(arguments.Uri.Value!),
                FileSystem.ResolvePath(arguments.FileName.Value()?.Trim(), "schema.graphql"),
                arguments.Token.Value()?.Trim(),
                arguments.Scheme.Value()?.Trim() ?? "bearer");

            FileSystem.EnsureDirectoryExists(
                FileSystem.GetDirectoryName(context.FileName));

            return await DownloadSchemaAsync(context) ? 0 : 1;
        }

        private async Task<bool> DownloadSchemaAsync(DownloadCommandContext context)
        {
            using var activity = Output.WriteActivity("Download schema");

            try
            {
                HttpClient client = HttpClientFactory.Create(
                    context.Uri, context.Token, context.Scheme);
                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(client);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                await FileSystem.WriteToAsync(context.FileName, stream =>
                    Task.Run(() => SchemaSyntaxSerializer.Serialize(
                        schema, stream, true)));
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
