using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.Config;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class InitCommandHandler
        : CommandHandler<InitCommandArguments>
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
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new InitCommandContext(
                arguments.Name.Value()?.Trim() ?? Path.GetDirectoryName(Environment.CurrentDirectory),
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
            if(context.Uri is null)
            {
                return true;
            }

            using IActivity activity = Output.WriteActivity("Download schema");

            string schemaFilePath = FileSystem.CombinePath(
                context.Path, context.SchemaFileName);
            string schemaExtensionFilePath = FileSystem.CombinePath(
                context.Path, context.SchemaExtensionFileName);

            HttpClient client = HttpClientFactory.Create(
                context.Uri, context.Token, context.Scheme);

            if (await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath,
                cancellationToken)
                .ConfigureAwait(false))
            {
                await FileSystem.WriteTextAsync(
                    schemaExtensionFilePath,
                    @"extend schema @key(fields: ""id"")")
                    .ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private async Task WriteConfigurationAsync(
           InitCommandContext context,
           CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Client configuration");

            string configFilePath = FileSystem.CombinePath(
                context.Path, context.ConfigFileName);

            var configuration = new GraphQLConfig
            {
                Schema = context.SchemaFileName,
                Documents = "**/*.graphql",
                Extensions = new()
                {
                    StrawberryShake = new()
                    {
                        Name = context.ClientName,
                        Namespace = context.CustomNamespace,
                        Url = context.Uri!.ToString(),
                        DependencyInjection = context.UseDependencyInjection
                    }
                }
            };

            await FileSystem.WriteTextAsync(
                configFilePath,
                JsonSerializer.Serialize(
                    configuration,
                    new()
                    {
                        WriteIndented = true,
                        IgnoreNullValues = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }))
                .ConfigureAwait(false);
        }
    }
}
