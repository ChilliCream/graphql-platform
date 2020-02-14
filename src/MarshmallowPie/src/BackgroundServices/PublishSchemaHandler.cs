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
    public class PublishSchemaHandler
        : IPublishDocumentHandler
    {
        private const string _fileName = "schema.graphql";
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;

        public PublishSchemaHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IMessageSender<PublishDocumentEvent> eventSender)
        {
            _fileStorage = fileStorage;
            _schemaRepository = schemaRepository;
            _eventSender = eventSender;
        }

        public async ValueTask<bool> CanHandleAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            if (message is { Type: DocumentType.Schema, ExternalId: { } })
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

            return HandleInternalAsync(message, cancellationToken);
        }

        private async Task HandleInternalAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            var issueLogger = new IssueLogger(message.SessionId, _eventSender);

            try
            {
                SchemaVersion? version = await _schemaRepository.GetSchemaVersionByExternalIdAsync(
                    message.ExternalId!, cancellationToken)
                    .ConfigureAwait(false);

                if (version is null)
                {
                    await issueLogger.LogIssueAsync(new Issue(
                        "PROCESSING_FAILED",
                        "There is now schema version associated with external " +
                        $"ID `{message.ExternalId}`.",
                        _fileName,
                        new Location(0, 0, 1, 1),
                        IssueType.Error,
                        ResolutionType.None))
                        .ConfigureAwait(false);
                    return;
                }

                IFileContainer fileContainer =
                    await _fileStorage.GetContainerAsync(
                        version.Id.ToString("N", CultureInfo.InvariantCulture))
                        .ConfigureAwait(false);

                IEnumerable<IFile> files =
                    await fileContainer.GetFilesAsync(cancellationToken).ConfigureAwait(false);

                IFile schemaFile = files.Single();

                DocumentNode? schemaDocument = await DocumentHelper.TryParseDocumentAsync(
                    schemaFile, issueLogger, cancellationToken)
                    .ConfigureAwait(false);

                string sourceText = await DocumentHelper.LoadSourceTextAsync(
                    schemaFile, schemaDocument, cancellationToken)
                    .ConfigureAwait(false);

                await PublishSchemaVersionAsync(
                    message,
                    version,
                    schemaDocument,
                    issueLogger,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await issueLogger.LogIssueAsync(new Issue(
                    "PROCESSING_FAILED",
                    "Internal processing error.",
                    _fileName,
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

        private static string PrintSchema(DocumentNode document) =>
            SchemaSyntaxSerializer.Serialize(document, true);

        private async Task PublishSchemaVersionAsync(
            PublishDocumentMessage message,
            SchemaVersion schemaVersion,
            DocumentNode? schemaDocument,
            IssueLogger issueLogger,
            CancellationToken cancellationToken)
        {
            if (schemaDocument is { })
            {
                ISchema? schema =
                    await SchemaHelper.TryCreateSchemaAsync(
                        schemaDocument, issueLogger, cancellationToken)
                        .ConfigureAwait(false);

                if (schema is { })
                {
                    // todo: start looking for incompatibilities
                }
            }

            var report = new SchemaPublishReport(
                schemaVersion.Id,
                message.EnvironmentId,
                issueLogger.Issues,
                PublishState.Published,
                DateTime.UtcNow);

            await _schemaRepository.SetPublishReportAsync(
                report, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
