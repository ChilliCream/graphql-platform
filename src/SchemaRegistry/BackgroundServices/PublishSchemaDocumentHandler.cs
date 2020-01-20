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
using MarshmallowPie;
using MarshmallowPie.Processing;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace BackgroundServices
{
    public class PublishSchemaDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;

        public PublishSchemaDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository,
            IMessageSender<PublishDocumentEvent> eventSender)
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
            try
            {
                IFile schemaFile =
                    await ResolveSchemaFileAsync(message, cancellationToken).ConfigureAwait(false);

                var issues = new List<Issue>();
                var issueLogger = new IssueLogger(message.SessionId, _eventSender, issues);

                DocumentNode? schemaDocument = await TryParseSchemaAsync(
                    schemaFile, issueLogger, cancellationToken)
                    .ConfigureAwait(false);

                if (schemaDocument is { })
                {
                    string formattedSourceText = PrintSchema(schemaDocument);
                    string hash = Hash.ComputeHash(formattedSourceText);

                    ISchema? schema = await TryCreateSchema(
                        schemaDocument, issueLogger, cancellationToken)
                        .ConfigureAwait(false);

                    if (schema is { })
                    {
                        // start looking for incompatibilities
                    }

                    SchemaVersion? schemaVersion = await _schemaRepository.GetSchemaVersionAsync(
                        hash, cancellationToken)
                        .ConfigureAwait(false);

                    if (schemaVersion is null)
                    {
                        schemaVersion = await CreateSchemaVersionAsync(
                            formattedSourceText,
                            message.Tags,
                            message.SchemaId,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        schemaVersion = await UpdateSchemaVersionAsync(
                            schemaVersion,
                            message.Tags,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }

                    await TryCreateReportAsync(
                        schemaVersion.Id,
                        message.EnvironmentId,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                await _eventSender.SendAsync(
                    PublishDocumentEvent.Completed(message.SessionId),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<IFile> ResolveSchemaFileAsync(
            PublishDocumentMessage message,
            CancellationToken cancellationToken)
        {
            IFileContainer fileContainer =
                await _fileStorage.GetContainerAsync(message.SessionId).ConfigureAwait(false);

            IEnumerable<IFile> files =
                await fileContainer.GetFilesAsync(cancellationToken).ConfigureAwait(false);

            return files.Single();
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

        private static string PrintSchema(DocumentNode document) =>
            SchemaSyntaxSerializer.Serialize(document, true);

        private async Task<ISchema?> TryCreateSchema(
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

        private async Task<SchemaPublishReport> TryCreateReportAsync(
            Guid schemaVersionId,
            Guid environmentId,
            CancellationToken cancellationToken)
        {
            SchemaPublishReport? report =
                await _schemaRepository.GetPublishReportAsync(
                    schemaVersionId,
                    environmentId,
                    cancellationToken)
                    .ConfigureAwait(false);

            if (report is null)
            {
                report = new SchemaPublishReport(
                    schemaVersionId,
                    environmentId,
                    Array.Empty<Issue>(),
                    PublishState.Published,
                    DateTime.UtcNow);

                await _schemaRepository.AddPublishReportAsync(
                    report, cancellationToken)
                    .ConfigureAwait(false);
            }

            return report;
        }
    }

    internal sealed class IssueLogger
    {
        private string _sessionId;
        private IMessageSender<PublishDocumentEvent> _eventSender;
        private readonly ICollection<Issue> _issues;

        public IssueLogger(
            string sessionId,
            IMessageSender<PublishDocumentEvent> eventSender,
            ICollection<Issue> issues)
        {
            _sessionId = sessionId;
            _eventSender = eventSender;
            _issues = issues;
        }

        public Task LogIssueAsync(
            string message,
            IssueType type,
            CancellationToken cancellationToken = default) =>
            LogIssueAsync(null, message, type, cancellationToken);

        public async Task LogIssueAsync(
            string? code,
            string message,
            IssueType type,
            CancellationToken cancellationToken = default)
        {
            var issue = new Issue(code, message, type, ResolutionType.Open);

            _issues.Add(issue);

            await _eventSender.SendAsync(
                new PublishDocumentEvent(_sessionId, issue),
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
