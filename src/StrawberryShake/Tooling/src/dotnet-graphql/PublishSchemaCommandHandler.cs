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
                arguments.ExternalId.Value!,
                arguments.SchemaFileName.Value(),
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
                return await PublishSchemaAsync(
                    context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<int> MarkAsPublishedAsync(
            PublishSchemaCommandContext context,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Mark schema version published");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);
            ISchemaRegistryClient client = clientFactory.Create();

            IOperationResult<IMarkSchemaPublished> result =
                await client.MarkSchemaPublishedAsync(
                    context.ExternalId,
                    context.SchemaName,
                    context.EnvironmentName,
                    cancellationToken)
                    .ConfigureAwait(false);
            result.EnsureNoErrors();
            return 0;
        }

        private async Task<int> PublishSchemaAsync(
            PublishSchemaCommandContext context,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Publish schema version");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);
            ISchemaRegistryClient client = clientFactory.Create();

            IOperationResult<IPublishSchema> result;

            if (context.SchemaFileName is { })
            {
                string sourceText = File.ReadAllText(context.SchemaFileName);

                result =
                    await client.PublishSchemaAsync(
                        context.ExternalId,
                        context.SchemaName,
                        context.EnvironmentName,
                        sourceText,
                        new Optional<IReadOnlyList<TagInput>?>(context.Tags),
                        cancellationToken)
                        .ConfigureAwait(false);
            }
            else
            {
                result =
                    await client.PublishSchemaAsync(
                        context.ExternalId,
                        context.SchemaName,
                        context.EnvironmentName,
                        default,
                        new Optional<IReadOnlyList<TagInput>?>(context.Tags),
                        cancellationToken)
                        .ConfigureAwait(false);
            }
            result.EnsureNoErrors();

            IResponseStream<IOnPublishDocument> responseStream =
                await client.OnPublishDocumentAsync(
                    result.Data!.PublishSchema.SessionId,
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
