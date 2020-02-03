using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishNewQueryDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IFileStorage _fileStorage;
        private readonly IClientRepository _clientRepository;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;
        private readonly IQueryValidationRule[] _validationRules;

        public PublishNewQueryDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IMessageSender<PublishDocumentEvent> eventSender,
            IEnumerable<IQueryValidationRule>? validationRules)
        {
            _fileStorage = fileStorage
                ?? throw new ArgumentNullException(nameof(fileStorage));
            _schemaRepository = schemaRepository
                ?? throw new ArgumentNullException(nameof(schemaRepository));
            _eventSender = eventSender
                ?? throw new ArgumentNullException(nameof(eventSender));
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

            return HandleInternalAsync(message, cancellationToken);
        }

        private async Task HandleInternalAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            var logger = new IssueLogger(message.SessionId, _eventSender);
            var documents = new List<DocumentInfo>();
            ISchema schema;

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



            await SaveQueryDocumentsAsync(
                documents, cancellationToken)
                .ConfigureAwait(false);
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
