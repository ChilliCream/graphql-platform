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
    public abstract class PublishNewQueryDocumentHandlerBase
        : IPublishDocumentHandler
    {
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;
        private readonly IQueryValidationRule[] _validationRules;

        public PublishNewQueryDocumentHandlerBase(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IClientRepository clientRepository,
            IMessageSender<PublishDocumentEvent> eventSender,
            IEnumerable<IQueryValidationRule>? validationRules)
        {
            FileStorage = fileStorage;
            SchemaRepository = schemaRepository;
            ClientRepository = clientRepository;
            _eventSender = eventSender;
            _validationRules = validationRules?.ToArray() ?? Array.Empty<IQueryValidationRule>();
        }

        protected IFileStorage FileStorage { get; }

        protected ISchemaRepository SchemaRepository { get; }

        protected IClientRepository ClientRepository { get; }

        public abstract ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken);

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
                var documents = new List<QueryDocumentInfo>();
                IReadOnlyList<Guid> queryIds = Array.Empty<Guid>();

                ISchema? schema =
                    await SchemaHelper.TryLoadSchemaAsync(
                        SchemaRepository,
                        FileStorage,
                        message.SchemaId,
                        message.EnvironmentId,
                        logger,
                        cancellationToken)
                        .ConfigureAwait(false);

                IFileContainer fileContainer = await FileStorage.GetContainerAsync(
                        message.SessionId, cancellationToken)
                        .ConfigureAwait(false);

                if (schema is { })
                {
                    queryIds = await ProcessDocumentsAsync(
                        message.SchemaId,
                        schema,
                        fileContainer,
                        message.Documents,
                        documents,
                        logger,
                        cancellationToken)
                        .ConfigureAwait(false);
                }

                Guid clientVersionId = await AddClientVersionAsync(
                    message, queryIds, cancellationToken)
                    .ConfigureAwait(false);

                await AddEnvironmentPublishReport(
                    clientVersionId, message.EnvironmentId, logger, cancellationToken)
                    .ConfigureAwait(false);

                await fileContainer.DeleteAsync(
                    cancellationToken)
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

        private async Task<IReadOnlyList<Guid>> ProcessDocumentsAsync(
            Guid schemaId,
            ISchema schema,
            IFileContainer fileContainer,
            IReadOnlyList<DocumentInfo> documentInfos,
            ICollection<QueryDocumentInfo> queryDocuments,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            foreach (DocumentInfo documentInfo in documentInfos)
            {
                IFile file = await fileContainer.GetFileAsync(
                    documentInfo.Name, cancellationToken)
                    .ConfigureAwait(false);

                await ProcessDocumentAsync(
                    schemaId,
                    schema,
                    file,
                    documentInfo,
                    queryDocuments,
                    logger,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            IFileContainer queryContainer = await FileStorage.GetOrCreateContainerAsync(
                $"{schemaId.ToString("N", CultureInfo.InvariantCulture)}_queries",
                cancellationToken)
                .ConfigureAwait(false);

            return await SaveQueryDocumentsAsync(
                schemaId, queryDocuments, queryContainer, cancellationToken)
                .ConfigureAwait(false);
        }

        protected abstract Task ProcessDocumentAsync(
            Guid schemaId,
            ISchema schema,
            IFile file,
            DocumentInfo documentInfo,
            ICollection<QueryDocumentInfo> queryDocuments,
            IssueLogger logger,
            CancellationToken cancellationToken);

        protected async Task ValidateQueryDocumentAsync(
            ISchema schema,
            string fileName,
            DocumentNode document,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _validationRules.Length; i++)
            {
                IEnumerable<Issue> issues =
                    await _validationRules[i].ValidateAsync(
                        schema, fileName, document, cancellationToken)
                        .ConfigureAwait(true);

                await logger.LogIssuesAsync(
                    issues, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<IReadOnlyList<Guid>> SaveQueryDocumentsAsync(
            Guid schemaId,
            IEnumerable<QueryDocumentInfo> documents,
            IFileContainer container,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, QueryDocument> queries =
                await ClientRepository.GetQueryDocumentsAsync(
                    schemaId,
                    documents.Select(t => t.Hash.Hash).ToArray(),
                    cancellationToken)
                    .ConfigureAwait(false);

            var newQueries = new List<QueryDocument>();
            var queryIds = new List<Guid>(queries.Select(t => t.Value.Id));

            foreach (QueryDocumentInfo document in documents)
            {
                if (!queries.ContainsKey(document.Hash.Hash))
                {
                    QueryDocument query =
                        document.ExternalHash is null
                            ? new QueryDocument(schemaId, document.Hash)
                            : new QueryDocument(schemaId, document.Hash, document.ExternalHash);

                    await container.CreateTextFileAsync(
                        query.Id, document.SourceText, cancellationToken)
                        .ConfigureAwait(false);

                    newQueries.Add(query);
                    queryIds.Add(query.Id);
                }
            }

            if (newQueries.Count > 0)
            {
                await ClientRepository.AddQueryDocumentAsync(
                    newQueries, cancellationToken)
                    .ConfigureAwait(false);
            }

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
                new HashSet<Guid>(queryIds).ToList(),
                message.Tags,
                DateTime.UtcNow);

            await ClientRepository.AddClientVersionAsync(
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

            await ClientRepository.SetPublishReportAsync(
                publishReport, cancellationToken)
                .ConfigureAwait(false);
        }

        protected sealed class QueryDocumentInfo
        {
            public QueryDocumentInfo(
                IFile file,
                DocumentNode? document,
                string sourceText,
                DocumentHash hash,
                DocumentHash? externalHash)
            {
                File = file;
                Document = document;
                SourceText = sourceText;
                Hash = hash;
                ExternalHash = externalHash;
            }

            public IFile File { get; }

            public DocumentNode? Document { get; }

            public string SourceText { get; }

            public DocumentHash Hash { get; }

            public DocumentHash? ExternalHash { get; }
        }
    }
}
