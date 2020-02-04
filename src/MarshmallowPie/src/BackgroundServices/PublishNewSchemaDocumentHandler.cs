using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    public class PublishNewSchemaDocumentHandler
        : IPublishDocumentHandler
    {
        private const string _fileName = "schema.graphql";
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;

        public PublishNewSchemaDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IMessageSender<PublishDocumentEvent> eventSender)
        {
            _fileStorage = fileStorage;
            _schemaRepository = schemaRepository;
            _eventSender = eventSender;
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
            var issueLogger = new IssueLogger(message.SessionId, _eventSender);

            try
            {
                IFileContainer fileContainer =
                    await _fileStorage.GetContainerAsync(message.SessionId).ConfigureAwait(false);

                IEnumerable<IFile> files =
                    await fileContainer.GetFilesAsync(cancellationToken).ConfigureAwait(false);

                IFile schemaFile = files.Single();

                DocumentNode? schemaDocument = await DocumentHelper.TryParseDocumentAsync(
                    schemaFile, issueLogger, cancellationToken)
                    .ConfigureAwait(false);

                string sourceText = await DocumentHelper.LoadSourceTextAsync(
                    schemaFile, schemaDocument, cancellationToken)
                    .ConfigureAwait(false);

                await PublishNewSchemaVersionAsync(
                    message,
                    schemaDocument,
                    sourceText,
                    issueLogger,
                    cancellationToken)
                    .ConfigureAwait(false);

                await fileContainer.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await issueLogger.LogIssueAsync(new Issue(
                    "PROCESSING_FAILED",
                    "Internal processing error.",
                    _fileName,
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

        private static string PrintSchema(DocumentNode document) =>
            SchemaSyntaxSerializer.Serialize(document, true);

        private static async Task<ISchema?> TryCreateSchema(
            DocumentNode document,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                return SchemaBuilder.New()
                    .AddDocument(sp => document)
                    .Use(next => context => Task.CompletedTask)
                    .Create();
            }
            catch (SchemaException ex)
            {
                foreach (ISchemaError error in ex.Errors)
                {
                    await logger.LogIssueAsync(
                        new Issue(
                            error.Code ?? "SCHEMA_ERROR",
                            error.Message,
                            _fileName,
                            error.SyntaxNodes.FirstOrDefault()?.Location
                                ?? new Location(0, 0, 0, 0),
                            IssueType.Error,
                            ResolutionType.CannotBeFixed),
                        cancellationToken)
                        .ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    new Issue(
                        "SCHEMA_ERROR",
                        ex.Message,
                        _fileName,
                        new Location(0, 0, 0, 0),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
        }

        private async Task<SchemaVersion> CreateSchemaVersionAsync(
           string sourceText,
           IReadOnlyList<Tag> tags,
           Guid schemaId,
           CancellationToken cancellationToken)
        {
            var schemaVersion = new SchemaVersion(
                schemaId,
                sourceText,
                DocumentHash.FromSourceText(sourceText),
                tags.Select(t => new Tag(t.Key, t.Value, DateTime.UtcNow)).ToList(),
                DateTime.UtcNow);

            await _schemaRepository.AddSchemaVersionAsync(
                schemaVersion, cancellationToken)
                .ConfigureAwait(false);

            return schemaVersion;
        }

        private async Task PublishNewSchemaVersionAsync(
            PublishDocumentMessage message,
            DocumentNode? schemaDocument,
            string formattedSourceText,
            IssueLogger issueLogger,
            CancellationToken cancellationToken)
        {
            if (schemaDocument is { })
            {
                ISchema? schema = await TryCreateSchema(
                    schemaDocument, issueLogger, cancellationToken)
                    .ConfigureAwait(false);

                if (schema is { })
                {
                    // todo: start looking for incompatibilities
                }
            }

            Guid versionId = Guid.NewGuid();

            IFileContainer container = await _fileStorage.CreateContainerAsync(
                versionId.ToString("N", CultureInfo.InvariantCulture),
                cancellationToken)
                .ConfigureAwait(false);

            using (Stream stream = await container.CreateFileAsync(
                "schema.graphql", cancellationToken)
                .ConfigureAwait(false))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(formattedSourceText);
                await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }

            SchemaVersion schemaVersion = await CreateSchemaVersionAsync(
                formattedSourceText,
                message.Tags,
                message.SchemaId,
                cancellationToken)
                .ConfigureAwait(false);

            var report = new SchemaPublishReport(
                schemaVersion.Id,
                message.EnvironmentId,
                issueLogger.Issues,
                PublishState.Published,
                DateTime.UtcNow);

            await _schemaRepository.AddPublishReportAsync(
                report, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
