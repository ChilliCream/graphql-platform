using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using HCErrorBuilder = HotChocolate.ErrorBuilder;
using System.Threading;

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
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            var context = new InitCommandContext(
                arguments.Schema.Value()?.Trim() ?? "schema",
                FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
                arguments.Token.Value()?.Trim(),
                arguments.Schema.Value()?.Trim() ?? "bearer",
                new Uri(arguments.Uri.Value!));

            FileSystem.EnsureDirectoryExists(context.Path);

            if (await DownloadSchemaAsync(context))
            {
                await WriteConfigurationAsync(context, cancellationToken);
                return 0;
            }

            return 1;
        }

        private async Task<bool> DownloadSchemaAsync(InitCommandContext context)
        {
            using var activity = Output.WriteActivity("Download schema");

            try
            {
                HttpClient client = HttpClientFactory.Create(
                    context.Uri, context.Token, context.Scheme);
                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(client);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                string schemaFilePath = FileSystem.CombinePath(
                    context.Path, context.SchemaFileName);
                await FileSystem.WriteToAsync(schemaFilePath, stream =>
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

        private async Task WriteConfigurationAsync(
           InitCommandContext context,
           CancellationToken cancellationToken)
        {
            using var activity = Output.WriteActivity("Client configuration");

            var configuration = ConfigurationStore.New();

            configuration.ClientName = context.ClientName;
            configuration.Schemas.Add(new SchemaFile
            {
                Type = "http",
                Name = context.SchemaName,
                File = context.SchemaFileName,
                Url = context.Uri.ToString()
            });

            await ConfigurationStore.SaveAsync(context.Path, configuration);
        }
    }
}
