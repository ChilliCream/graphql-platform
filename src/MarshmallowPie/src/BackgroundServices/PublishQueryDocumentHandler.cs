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
    public class PublishQueryDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;
        private readonly IQueryValidationRule[] _validationRules;
        private IFileStorage _fileStorage;
        private ISchemaRepository _schemaRepository;
        private IClientRepository _clientRepository;

        public PublishQueryDocumentHandler(
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

        public async ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            if (message is { Type: DocumentType.Query, ExternalId: { } }
                || message is { Type: DocumentType.Relay, ExternalId: { } })
            {
                return !await _fileStorage.ContainerExistsAsync(
                    message.SessionId, cancellationToken)
                    .ConfigureAwait(false);
            }
            return false;
        }

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
                ClientVersion? version =
                    await _clientRepository.GetClientVersionByExternalIdAsync(
                        message.ExternalId!, cancellationToken)
                        .ConfigureAwait(false);

                if (version is null)
                {
                    await logger.LogIssueAsync(new Issue(
                        "PROCESSING_FAILED",
                        "There is now client version associated with external " +
                        $"ID `{message.ExternalId}`.",
                        "query.graphql",
                        new Location(0, 0, 1, 1),
                        IssueType.Error,
                        ResolutionType.None))
                        .ConfigureAwait(false);
                    return;
                }

                IFileContainer queryContainer = await _fileStorage.GetOrCreateContainerAsync(
                    $"{message.SchemaId.ToString("N", CultureInfo.InvariantCulture)}_queries",
                    cancellationToken)
                    .ConfigureAwait(false);

                ISchema? schema = await SchemaHelper.TryLoadSchemaAsync(
                    _schemaRepository,
                    _fileStorage,
                    message.SchemaId,
                    message.EnvironmentId,
                    logger,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (schema is { })
                {
                    await ProcessDocumentsAsync(
                        schema,
                        version,
                        queryContainer,
                        logger,
                        cancellationToken)
                        .ConfigureAwait(false);
                }

                await SetEnvironmentPublishReport(
                    version.Id, message.EnvironmentId, logger, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await logger.LogIssueAsync(new Issue(
                    "PROCESSING_FAILED",
                    "Internal processing error.",
                    "schema.graphql",
                    new Location(0, 0, 1, 1),
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

        private async Task ProcessDocumentsAsync(
            ISchema schema,
            ClientVersion clientVersion,
            IFileContainer fileContainer,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, QueryDocument> documents =
                await _clientRepository.GetQueryDocumentsAsync(
                    clientVersion.QueryIds,
                    cancellationToken)
                    .ConfigureAwait(false);

            foreach (Guid queryId in clientVersion.QueryIds)
            {
                QueryDocument document = documents[queryId];
                string sourceText = await fileContainer
                    .ReadAllTextAsync(queryId)
                    .ConfigureAwait(false);
                DocumentHash hash = document.ExternalHashes.Count > 0
                    ? document.ExternalHashes.First()
                    : document.Hash;

                await ProcessDocumentAsync(
                    schema,
                    hash.Hash,
                    sourceText,
                    logger,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentAsync(
            ISchema schema,
            string fileName,
            string sourceText,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            DocumentNode? document =
               await DocumentHelper.TryParseDocumentAsync(
                   fileName, sourceText, logger, cancellationToken)
                   .ConfigureAwait(false);

            if (document is { })
            {
                await ValidateQueryDocumentAsync(
                    schema, fileName, document, logger, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

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

        private async Task SetEnvironmentPublishReport(
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

            await _clientRepository.SetPublishReportAsync(
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
