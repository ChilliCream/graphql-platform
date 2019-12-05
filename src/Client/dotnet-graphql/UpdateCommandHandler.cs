using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Language;
using HCErrorBuilder = HotChocolate.ErrorBuilder;
using System.Threading;

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

        public override Task<int> ExecuteAsync(
            UpdateCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            var context = new UpdateCommandContext(
                arguments.Uri.HasValue() ? new Uri(arguments.Uri.Value()?.Trim()) : null,
                arguments.Path.Value()?.Trim(),
                arguments.Token.Value()?.Trim(),
                arguments.Scheme.Value()?.Trim() ?? "bearer");

            return context.Path is null
                ? FindAndUpdateSchemasAsync(context)
                : UpdateSingleSchemaAsync(context, context.Path);
        }

        private async Task<int> FindAndUpdateSchemasAsync(UpdateCommandContext context)
        {
            foreach (string path in FileSystem.GetClientDirectories(FileSystem.CurrentDirectory))
            {
                try
                {
                    await UpdateSingleSchemaAsync(context, path);
                }
                catch
                {
                    return 1;
                }
            }
            return 0;
        }

        private async Task<int> UpdateSingleSchemaAsync(UpdateCommandContext context, string path)
        {
            Configuration? configuration = await ConfigurationStore.TryLoadAsync(context.Path!);

            if (configuration is { }
                && await UpdateSchemaAsync(context, context.Path! ?? path, configuration))
            {
                return 0;
            }

            return 1;
        }

        private async Task<bool> UpdateSchemaAsync(
            UpdateCommandContext context,
            string path,
            Configuration configuration)
        {
            bool hasErrors = false;

            foreach (SchemaFile schema in configuration.Schemas!)
            {
                if (schema.Type == "http")
                {
                    if (!await DownloadSchemaAsync(context, path, schema))
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
            SchemaFile schemaFile)
        {
            using var activity = Output.WriteActivity("Download schema");

            try
            {
                HttpClient client = HttpClientFactory.Create(
                    context.Uri ?? new Uri(schemaFile.Url),
                    context.Token,
                    context.Scheme);

                DocumentNode schema = await IntrospectionClient.LoadSchemaAsync(client);
                schema = IntrospectionClient.RemoveBuiltInTypes(schema);

                string schemaFilePath = FileSystem.CombinePath(
                    path, schemaFile.File);
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
    }
}
