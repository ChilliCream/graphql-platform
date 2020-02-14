using System.IO;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.OAuth;
using StrawberryShake.Tools.SchemaRegistry;
using System.Collections.Generic;
using HCErrorBuilder = HotChocolate.ErrorBuilder;
using HCLocation = HotChocolate.Location;

namespace StrawberryShake.Tools
{
    public class PublishClientCommandHandler
        : CommandHandler<PublishClientCommandArguments>
    {
        public PublishClientCommandHandler(
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
            PublishClientCommandArguments arguments,
            CancellationToken cancellationToken)
        {
            using IDisposable command = Output.WriteCommand();

            AccessToken? accessToken =
                await arguments.AuthArguments
                    .RequestTokenAsync(Output, cancellationToken)
                    .ConfigureAwait(false);

            var context = new PublishClientCommandContext(
                new Uri(arguments.Registry.Value!),
                arguments.EnvironmentName.Value!,
                arguments.SchemaName.Value!,
                arguments.ClientName.Value!,
                arguments.ExternalId.Value!,
                arguments.SearchDirectory.Value()?.Trim() ?? FileSystem.CurrentDirectory,
                arguments.QueryFileName.Values.Where(t => t is { }).ToList()!,
                arguments.RelayFileFormat.HasValue(),
                arguments.Tag.HasValue()
                    ? arguments.Tag.Values
                        .Where(t => t! is { })
                        .Select(t => t!.Split('='))
                        .Select(t => new TagInput { Key = t[0], Value = t[1] })
                        .ToList()
                    : null,
                arguments.Published.HasValue(),
                accessToken?.Token,
                accessToken?.Scheme);

            if (context.Published)
            {
                return await MarkAsPublishedAsync(
                    context, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                return await PublishClientAsync(
                    context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<int> MarkAsPublishedAsync(
            PublishClientCommandContext context,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Mark client version published");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);
            ISchemaRegistryClient client = clientFactory.Create();

            IOperationResult<IMarkClientPublished> result =
                await client.MarkClientPublishedAsync(
                    context.ExternalId,
                    context.SchemaName,
                    context.EnvironmentName,
                    cancellationToken)
                    .ConfigureAwait(false);
            result.EnsureNoErrors();
            return 0;
        }

        private async Task<int> PublishClientAsync(
            PublishClientCommandContext context,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Publish client version");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);
            ISchemaRegistryClient client = clientFactory.Create();

            var files = new List<QueryFileInput>();

            foreach (string fileName in context.QueryFileNames.SelectMany(filter =>
                Directory.GetFiles(context.SearchDirectory, filter)))
            {
                files.Add(new QueryFileInput
                {
                    Name = Path.GetFileName(fileName),
                    SourceText = File.ReadAllText(fileName)
                });
            }

            IOperationResult<IPublishClient> result =
                await client.PublishClientAsync(
                    context.ExternalId,
                    context.SchemaName,
                    context.EnvironmentName,
                    context.ClientName,
                    context.RelayFileFormat
                        ? QueryFileFormat.Relay
                        : QueryFileFormat.Graphql,
                    files,
                    new Optional<IReadOnlyList<TagInput>?>(context.Tags),
                    cancellationToken)
                    .ConfigureAwait(false);
            result.EnsureNoErrors();

            IResponseStream<IOnPublishDocument> responseStream =
                await client.OnPublishDocumentAsync(
                    result.Data!.PublishClient.SessionId,
                    cancellationToken)
                    .ConfigureAwait(false);

            bool hasErrors = false;

            await foreach (IOperationResult<IOnPublishDocument> documentEvent in
                responseStream.WithCancellation(cancellationToken))
            {
                if (documentEvent.Data is null
                    || documentEvent.Data.OnPublishDocument.IsCompleted)
                {
                    break;
                }

                if (documentEvent.Data?.OnPublishDocument.Issue is { } issue
                    && issue.Type == IssueType.Error)
                {
                    hasErrors = true;
                    activity.WriteError(
                        HCErrorBuilder.New()
                            .SetCode(issue.Code)
                            .SetMessage(issue.Message)
                            .AddLocation(new HCLocation(
                                issue.Location.Line,
                                issue.Location.Column))
                            .SetExtension("fileName", issue.File)
                            .Build());
                }
            }

            return hasErrors ? 1 : 0;
        }
    }
}
