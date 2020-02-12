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
                arguments.SchemaFileName.Value!,
                arguments.Tag.HasValue()
                    ? arguments.Tag.Values
                        .Where(t => t! is { })
                        .Select(t => t!.Split('='))
                        .Select(t => new TagInput { Key = t[0], Value = t[1] })
                        .ToList()
                    : null,
                accessToken?.Token,
                accessToken?.Scheme);

            return await PublishSchemaAsync(
                context, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<int> PublishSchemaAsync(
            PublishSchemaCommandContext context,
            CancellationToken cancellationToken)
        {
            using IActivity activity = Output.WriteActivity("Publish schema");

            var clientFactory = new SchemaRegistryClientFactory(
                context.Registry, context.Token, context.Scheme);
            ISchemaRegistryClient client = clientFactory.Create();

            string sourceText = File.ReadAllText(context.SchemaFileName);

            IOperationResult<IPublishSchema> result =
                await client.PublishSchemaAsync(
                    context.ExternalId,
                    context.SchemaName,
                    context.EnvironmentName,
                    sourceText,
                    new Optional<IReadOnlyList<TagInput>?>(context.Tags),
                    cancellationToken)
                    .ConfigureAwait(false);
            result.EnsureNoErrors();

            IResponseStream<IOnPublishDocument> responseStream =
                await client.OnPublishDocumentAsync(
                    result.Data!.PublishSchema.SessionId,
                    cancellationToken)
                    .ConfigureAwait(false);

            bool hasErrors = false;

            await foreach (IOnPublishDocument documentEvent in
                responseStream.WithCancellation(cancellationToken))
            {
                if (documentEvent.OnPublishDocument.IsCompleted)
                {
                    break;
                }

                if (documentEvent.OnPublishDocument.Issue is { } issue
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
