using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;
using Location = HotChocolate.Language.Location;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishNewQueryDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;
        private readonly IQueryValidationRule[] _validationRules;

        public PublishNewQueryDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IClientRepository clientRepository,
            IMessageSender<PublishDocumentEvent> eventSender,
            IEnumerable<IQueryValidationRule>? validationRules)
        {
            _fileStorage = fileStorage;
            _schemaRepository = schemaRepository;
            _clientRepository = clientRepository;
            _eventSender = eventSender;
            _validationRules = validationRules?.ToArray() ?? Array.Empty<IQueryValidationRule>();
        }

        public DocumentType Type => DocumentType.Schema;

        public Task HandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.ClientId is null)
            {
                throw new ArgumentException(
                    "The client id is not allowed to be null.",
                    nameof(message));
            }

            return HandleInternalAsync(message, cancellationToken);
        }

        private async Task HandleInternalAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            var logger = new IssueLogger(message.SessionId, _eventSender);

            try
            {
                var documents = new List<DocumentInfo>();
                IReadOnlyList<Guid> queryIds = Array.Empty<Guid>();
                ISchema? schema = await TryLoadSchemaAsync(
                    message.SchemaId, message.EnvironmentId, logger, cancellationToken)
                    .ConfigureAwait(false);

                if (schema is { })
                {
                    IFileContainer fileContainer =
                        await _fileStorage.GetContainerAsync(message.SessionId).ConfigureAwait(false);
                    IEnumerable<IFile> files =
                        await fileContainer.GetFilesAsync(cancellationToken).ConfigureAwait(false);

                    foreach (IFile file in files)
                    {
                        DocumentNode? document = await DocumentHelper.TryParseDocumentAsync(
                            file, logger, cancellationToken)
                            .ConfigureAwait(false);

                        string sourceText = await DocumentHelper.LoadSourceTextAsync(
                            file, document, cancellationToken)
                            .ConfigureAwait(false);

                        DocumentHash hash = DocumentHash.FromSourceText(sourceText);

                        if (document is { })
                        {
                            await ValidateQueryDocumentAsync(
                                schema, file, document, logger, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        documents.Add(new DocumentInfo(file, document, sourceText, hash));
                    }

                    IFileContainer queryContainer = await _fileStorage.GetOrCreateContainerAsync(
                        $"{message.SchemaId.ToString("N", CultureInfo.InvariantCulture)}_queries",
                        cancellationToken)
                        .ConfigureAwait(false);

                    queryIds = await SaveQueryDocumentsAsync(
                        documents, queryContainer, cancellationToken)
                        .ConfigureAwait(false);
                }

                Guid clientVersionId = await AddClientVersionAsync(
                    message, queryIds, cancellationToken)
                    .ConfigureAwait(false);

                await AddEnvironmentPublishReport(
                    clientVersionId, message.EnvironmentId, logger, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await logger.LogIssueAsync(new Issue(
                    "PROCESSING_FAILED",
                    "Internal processing error.",
                    "schema.graphql",
                    new Location(0, 0, 0, 0),
                    IssueType.Error,
                    ResolutionType.None))
                    .ConfigureAwait(false);
                throw;
            }
            finally
            {
                await _eventSender.SendAsync(
                    PublishDocumentEvent.Completed(message.SessionId),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ValidateQueryDocumentAsync(
            ISchema schema,
            IFile file,
            DocumentNode document,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _validationRules.Length; i++)
            {
                IEnumerable<Issue> issues =
                    await _validationRules[i].ValidateAsync(
                        schema, document, cancellationToken)
                        .ConfigureAwait(true);

                await logger.LogIssuesAsync(
                    issues, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<IReadOnlyList<Guid>> SaveQueryDocumentsAsync(
            IEnumerable<DocumentInfo> documents,
            IFileContainer container,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, Query> queries =
                await _clientRepository.GetQueriesAsync(
                    documents.Select(t => t.Hash.Hash).ToArray(),
                    cancellationToken)
                    .ConfigureAwait(false);

            var newQueries = new List<Query>();
            var queryIds = new List<Guid>(queries.Select(t => t.Value.Id));

            foreach (DocumentInfo document in documents)
            {
                if (!queries.ContainsKey(document.Hash.Hash))
                {
                    var query = new Query(document.Hash);

                    await container.CreateTextFileAsync(
                        query.Id, document.SourceText, cancellationToken)
                        .ConfigureAwait(false);

                    newQueries.Add(query);
                    queryIds.Add(query.Id);
                }
            }

            await _clientRepository.AddQueriesAsync(
                newQueries, cancellationToken)
                .ConfigureAwait(false);

            return queryIds;
        }

        private async Task<Guid> AddClientVersionAsync(
            PublishDocumentMessage message,
            IReadOnlyList<Guid> queryIds,
            CancellationToken cancellationToken)
        {
            var clientVersion = new ClientVersion(
                message.ClientId!.Value,
                message.ExternalId,
                new HashSet<Guid>(queryIds),
                message.Tags,
                DateTime.UtcNow);

            await _clientRepository.AddClientVersionAsync(
                clientVersion, cancellationToken)
                .ConfigureAwait(false);

            return clientVersion.Id;
        }

        private async Task AddEnvironmentPublishReport(
            Guid clientVersionId,
            Guid environmentId,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            var publishReport = new ClientPublishReport(
                clientVersionId,
                environmentId,
                logger.Issues,
                logger.Issues.Any(t => t.Type == IssueType.Error)
                    ? PublishState.Rejected
                    : PublishState.Published,
                DateTime.UtcNow);

            await _clientRepository.AddPublishReportAsync(
                publishReport, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<ISchema?> TryLoadSchemaAsync(
            Guid schemaId,
            Guid environmentId,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                PublishedSchema publishedSchema = await _schemaRepository.GetPublishedSchemaAsync(
                    schemaId, environmentId, cancellationToken)
                    .ConfigureAwait(false);

                IFileContainer container = await _fileStorage.GetContainerAsync(
                    publishedSchema.SchemaVersionId.ToString("N", CultureInfo.InvariantCulture),
                    cancellationToken)
                    .ConfigureAwait(false);

                IEnumerable<IFile> files = await container.GetFilesAsync(
                    cancellationToken)
                    .ConfigureAwait(false);

                DocumentNode? document = await DocumentHelper.TryParseDocumentAsync(
                    files.Single(), logger, cancellationToken)
                    .ConfigureAwait(false);

                if (document is { })
                {
                    // TODO : add custom scalar support => we need to be able to configure the supported literals for that.
                    return SchemaBuilder.New()
                        .AddDocument(sp => document)
                        .Use(next => context => Task.CompletedTask)
                        .Create();
                }
            }
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    new Issue(
                        "SCHEMA_ERROR",
                        ex.Message,
                        "schema.graphql",
                        new Location(0, 0, 0, 0),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            return null;
        }

        private class DocumentInfo
        {
            public DocumentInfo(
                IFile file,
                DocumentNode? document,
                string sourceText,
                DocumentHash hash)
            {
                File = file;
                Document = document;
                SourceText = sourceText;
                Hash = hash;
            }

            public IFile File { get; }

            public DocumentNode? Document { get; }

            public string SourceText { get; }

            public DocumentHash Hash { get; }
        }
    }
}
