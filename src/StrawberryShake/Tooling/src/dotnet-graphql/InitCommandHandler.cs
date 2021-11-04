using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.Configuration;
using StrawberryShake.Tools.OAuth;
using static StrawberryShake.Tools.Configuration.FileContents;

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

            Dictionary<string, IEnumerable<string>> headers = ParseHeadersArgument(arguments.CustomHeaders.Values);

            var context = new InitCommandContext(
                arguments.Name.Value()?.Trim() ?? Path.GetFileName(Environment.CurrentDirectory),
                FileSystem.ResolvePath(arguments.Path.Value()?.Trim()),
                new Uri(arguments.Uri.Value!),
                accessToken?.Token,
                accessToken?.Scheme,
                headers);

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
                context.Uri, context.Token, context.Scheme, context.CustomHeaders);

            if (await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, schemaFilePath,
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
           InitCommandContext context,
           CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Client configuration");

            string configFilePath = FileSystem.CombinePath(
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
                        Url = context.Uri!.ToString(),
                        DependencyInjection = context.UseDependencyInjection
                    }
                }
            };

            await FileSystem.WriteTextAsync(
                configFilePath,
                configuration.ToString())
                .ConfigureAwait(false);
        }

        private static Dictionary<string, IEnumerable<string>> ParseHeadersArgument(List<string?> arguments)
        {
            var headers = new Dictionary<string, IEnumerable<string>>();

            foreach (var argument in arguments)
            {
                var argumentParts = argument?.Trim().Split("=");
                if (argumentParts?.Length != 2) {
                    continue;
                }

                var argumentKey = argumentParts[0];

                var argumentValueParts = argumentParts[1].Trim().Split(",");

                _ = headers.TryAdd(argumentKey, argumentValueParts);
            }

            return headers;
        }
    }
}
