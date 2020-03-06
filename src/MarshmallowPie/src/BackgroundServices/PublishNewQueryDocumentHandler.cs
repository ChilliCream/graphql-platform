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
    public class PublishNewQueryDocumentHandler
        : PublishNewQueryDocumentHandlerBase
    {
        public PublishNewQueryDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IClientRepository clientRepository,
            IMessageSender<PublishDocumentEvent> eventSender,
            IEnumerable<IQueryValidationRule>? validationRules)
            : base(fileStorage, schemaRepository, clientRepository, eventSender, validationRules)
        {
        }

        public override async ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            if (message is { Type: DocumentType.Query })
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
            DocumentNode? document =
               await DocumentHelper.TryParseDocumentAsync(
                   file, logger, cancellationToken)
                   .ConfigureAwait(false);

            string sourceText =
                await DocumentHelper.LoadSourceTextAsync(
                    file, document, cancellationToken)
                    .ConfigureAwait(false);

            DocumentHash hash = DocumentHash.FromSourceText(sourceText);
            DocumentHash? externalHash = null;

            if (documentInfo.Hash is { })
            {
                string algorithm = documentInfo.HashAlgorithm?.ToUpperInvariant() ?? "MD5";
                HashFormat format = documentInfo.HashFormat ?? HashFormat.Hex;
                externalHash = new DocumentHash(documentInfo.Hash, algorithm, format);
            }

            if (document is { })
            {
                string fileName = (externalHash ?? hash).Hash;
                await ValidateQueryDocumentAsync(
                    schema, fileName, document, logger, cancellationToken)
                    .ConfigureAwait(false);
            }

            queryDocuments.Add(new QueryDocumentInfo(
                file, document, sourceText, hash, externalHash));
        }
    }
}
