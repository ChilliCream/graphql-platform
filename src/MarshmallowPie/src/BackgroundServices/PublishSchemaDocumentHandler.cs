using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace MarshmallowPie.BackgroundServices
{
    public class PublishSchemaDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IMessageSender<PublishSchemaEvent> _eventSender;

        public PublishSchemaDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IMessageSender<PublishSchemaEvent> eventSender)
        {
            _fileStorage = fileStorage
                ?? throw new ArgumentNullException(nameof(fileStorage));
            _schemaRepository = schemaRepository
                ?? throw new ArgumentNullException(nameof(schemaRepository));
            _eventSender = eventSender
                ?? throw new ArgumentNullException(nameof(eventSender));
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

                DocumentNode? schemaDocument = await TryParseSchemaAsync(
                    schemaFile, issueLogger, cancellationToken)
                    .ConfigureAwait(false);

                string formattedSourceText = schemaDocument is { }
                    ? PrintSchema(schemaDocument)
                    : await ReadSchemaSourceTextAsync(
                        schemaFile, cancellationToken)
                        .ConfigureAwait(false);
                string hash = Hash.ComputeHash(formattedSourceText);

                SchemaVersion? schemaVersion =
                    await _schemaRepository.GetSchemaVersionAsync(
                        hash, cancellationToken)
                        .ConfigureAwait(false);

                if (schemaVersion is null)
                {
                    await PublishNewSchemaVersionAsync(
                        message,
                        schemaDocument,
                        formattedSourceText,
                        issueLogger,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await PublishExistingSchemaVersionAsync(
                        message,
                        schemaDocument,
                        schemaVersion,
                        issueLogger,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                await issueLogger.LogIssueAsync(new Issue(
                    "PROCESSING_FAILED",
                    "Internal processing error.",
                    IssueType.Error,
                    ResolutionType.None))
                    .ConfigureAwait(false);
                throw;
            }
            finally
            {
                await _eventSender.SendAsync(
                    PublishSchemaEvent.Completed(message.SessionId),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static async Task<DocumentNode?> TryParseSchemaAsync(
            IFile file,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = await file.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);
                using var memoryStream = new MemoryStream();

                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                return Utf8GraphQLParser.Parse(memoryStream.ToArray());
            }
            catch (SyntaxException ex)
            {
                await logger.LogIssueAsync(
                    "LANG", ex.Message, IssueType.Error, cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    ex.Message, IssueType.Error, cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
#pragma warning restore CA1031
        }

        private async Task<string> ReadSchemaSourceTextAsync(
            IFile file,
            CancellationToken cancellationToken)
        {
            using (Stream stream = await file.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
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
                        error.Code, error.Message, IssueType.Error, cancellationToken)
                        .ConfigureAwait(false);
                }
                return null;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    ex.Message, IssueType.Error, cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
#pragma warning restore CA1031
        }

        private async Task<SchemaVersion> CreateSchemaVersionAsync(
           string sourceText,
           IReadOnlyList<Tag> tags,
           Guid schemaId,
           CancellationToken cancellationToken)
        {
            using var sha = SHA256.Create();
            string hash = Convert.ToBase64String(sha.ComputeHash(
                Encoding.UTF8.GetBytes(sourceText)));

            var schemaVersion = new SchemaVersion(
                schemaId,
                sourceText,
                hash,
                tags.Select(t => new Tag(t.Key, t.Value, DateTime.UtcNow)).ToList(),
                DateTime.UtcNow);

            await _schemaRepository.AddSchemaVersionAsync(
                schemaVersion, cancellationToken)
                .ConfigureAwait(false);

            return schemaVersion;
        }

        private async Task<SchemaVersion> UpdateSchemaVersionAsync(
            SchemaVersion schemaVersion,
            IReadOnlyList<Tag> tags,
            CancellationToken cancellationToken)
        {
            if (tags is { })
            {
                var list = new List<Tag>(schemaVersion.Tags);
                list.AddRange(tags.Select(t => new Tag(
                    t.Key, t.Value, DateTime.UtcNow)));

                schemaVersion = new SchemaVersion(
                    schemaVersion.Id,
                    schemaVersion.SchemaId,
                    schemaVersion.SourceText,
                    schemaVersion.Hash,
                    list,
                    schemaVersion.Published);

                await _schemaRepository.UpdateSchemaVersionAsync(
                    schemaVersion, cancellationToken)
                    .ConfigureAwait(false);
            }

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
                    // start looking for incompatibilities
                }
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

        private async Task PublishExistingSchemaVersionAsync(
            PublishDocumentMessage message,
            DocumentNode? schemaDocument,
            SchemaVersion schemaVersion,
            IssueLogger issueLogger,
            CancellationToken cancellationToken)
        {
            ISchema? schema = null;

            if (schemaDocument is { })
            {
                schema = await TryCreateSchema(
                   schemaDocument, issueLogger, cancellationToken)
                   .ConfigureAwait(false);
            }

            schemaVersion = await UpdateSchemaVersionAsync(
                schemaVersion,
                message.Tags,
                cancellationToken)
                .ConfigureAwait(false);

            SchemaPublishReport? report =
                await _schemaRepository.GetPublishReportAsync(
                    schemaVersion.Id,
                    message.EnvironmentId,
                    cancellationToken)
                    .ConfigureAwait(false);

            if (report is { })
            {
                foreach (Issue issue in report.Issues.Where(t =>
                    t.Resolution == ResolutionType.Open
                    || t.Resolution == ResolutionType.CannotBeFixed))
                {
                    await issueLogger.LogIssueAsync(
                        issue, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (schema is { })
            {
                // start looking for incompatibilities

                await _schemaRepository.AddPublishReportAsync(
                    new SchemaPublishReport(
                        schemaVersion.Id,
                        message.EnvironmentId,
                        issueLogger.Issues,
                        PublishState.Published,
                        DateTime.UtcNow),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
