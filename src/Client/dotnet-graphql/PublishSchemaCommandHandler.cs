using System.IO;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.OAuth;
using StrawberryShake.Tools.SchemaRegistry;
using System.Collections.Generic;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandHandler
        : CommandHandler<PublishSchemaCommandArguments>
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
            PublishSchemaCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new PublishSchemaCommandContext(
                new Uri(arguments.Registry.Value!),
                arguments.EnvironmentName.Value!,
                arguments.SchemaName.Value!,
                arguments.SchemaFileName.Value!,
                arguments.Tag.HasValue()
                    ? arguments.Tag.Values
                        .Where(t => t! is { })
                        .Select(t => t!.Split("="))
                        .Select(t => new TagInput { Key = t[0], Value = t[1] })
                        .ToList()
                    : null,
                accessToken?.Token,
                accessToken?.Scheme);

            await PublishSchemaAsync(context, cancellationToken);
            return 0;
        }

        private async Task PublishSchemaAsync(
            PublishSchemaCommandContext context,
            CancellationToken cancellationToken)
        {
            using var activity = Output.WriteActivity("Publish schema");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);

            ISchemaRegistryClient client = clientFactory.Create();

            string sourceText = await File.ReadAllTextAsync(context.SchemaFileName);

            IOperationResult<IPublishSchema> result =
                await client.PublishSchemaAsync(
                    context.SchemaName,
                    context.EnvironmentName,
                    sourceText,
                    new Optional<IReadOnlyList<TagInput>?>(context.Tags),
                    cancellationToken);
            result.EnsureNoErrors();
        }
    }
}
