using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishNewRelayDocumentHandler
        : PublishNewQueryDocumentHandlerBase
    {
        public PublishNewRelayDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IClientRepository clientRepository,
            IMessageSender<PublishDocumentEvent> eventSender,
            IEnumerable<IQueryValidationRule>? validationRules)
            : base(fileStorage, schemaRepository, clientRepository, eventSender, validationRules)
        {
        }

        public async override ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            if (message is { Type: DocumentType.Relay })
            {
                return await FileStorage.ContainerExistsAsync(
                    message.SessionId, cancellationToken)
                    .ConfigureAwait(false);
            }
            return false;
        }

        protected override async Task ProcessDocumentAsync(
            Guid schemaId,
            ISchema schema,
            IFile file,
            DocumentInfo documentInfo,
            ICollection<QueryDocumentInfo> queryDocuments,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            RelayDocument? relayDocument =
                await DocumentHelper.TryParseRelayDocumentAsync(
                    file, documentInfo, logger, cancellationToken)
                    .ConfigureAwait(false);

            if (relayDocument is { })
            {
                foreach (RelayQuery query in relayDocument.Queries)
                {
                    DocumentNode? document =
                        await DocumentHelper.TryParseDocumentAsync(
                            query.Hash.Hash, query.SourceText, logger, cancellationToken)
                            .ConfigureAwait(false);

                    DocumentHash hash = DocumentHash.FromSourceText(query.SourceText);

                    if (document is { })
                    {
                        await ValidateQueryDocumentAsync(
                            schema, query.Hash.Hash, document, logger, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    queryDocuments.Add(new QueryDocumentInfo(
                        file, document, query.SourceText, hash, query.Hash));
                }
            }
        }
    }
}
