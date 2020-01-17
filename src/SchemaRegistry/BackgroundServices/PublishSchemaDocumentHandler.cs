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
using MarshmallowPie.Messaging;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace BackgroundServices
{
    public class PublishSchemaDocumentHandler
        : IPublishDocumentHandler
    {
        private readonly IFileStorage _fileStorage;
        private readonly ISchemaRepository _schemaRepository;

        public PublishSchemaDocumentHandler(
            IFileStorage fileStorage,
            ISchemaRepository schemaRepository)
        {
            _fileStorage = fileStorage
                ?? throw new ArgumentNullException(nameof(fileStorage));
            _schemaRepository = schemaRepository
                ?? throw new ArgumentNullException(nameof(schemaRepository));
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
            IFile schemaFile =
                await ResolveSchemaFileAsync(message, cancellationToken).ConfigureAwait(false);

            var issues = new List<Issue>();
            DocumentNode? schemaDocument = await TryParseSchemaAsync(
                schemaFile, issues, cancellationToken)
                .ConfigureAwait(false);

            if (schemaDocument is { })
            {
                string formattedSourceText = PrintSchema(schemaDocument);
                string hash = Hash.ComputeHash(formattedSourceText);

                if (TryCreateSchema(schemaDocument, issues, out ISchema? schema))
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
            ICollection<Issue> issues,
            CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = await file.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var memoryStream = new MemoryStream();

                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                return Utf8GraphQLParser.Parse(memoryStream.ToArray());
            }
            catch (SyntaxException ex)
            {
                issues.Add(new Issue("LANG", ex.Message, IssueType.Error));
                return null;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                issues.Add(new Issue(ex.Message, IssueType.Error));
                return null;
            }
#pragma warning restore CA1031
        }

        private static string PrintSchema(DocumentNode document) =>
            SchemaSyntaxSerializer.Serialize(document, true);

        private bool TryCreateSchema(
            DocumentNode document,
            ICollection<Issue> issues,
            out ISchema? schema)
        {
            try
            {
                schema = SchemaBuilder.New()
                    .AddDocument(sp => document)
                    .Use(next => context => Task.CompletedTask)
                    .Create();
                return true;
            }
            catch (SchemaException ex)
            {
                foreach (Issue issue in ex.Errors.Select(t =>
                    new Issue(t.Code, t.Message, IssueType.Error)))
                {
                    issues.Add(issue);
                }
                schema = null;
                return false;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                issues.Add(new Issue(ex.Message, IssueType.Error));
                schema = null;
                return false;
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


    internal static class Hash
    {
        public static string ComputeHash(string formattedSourceText)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(
                Encoding.UTF8.GetBytes(formattedSourceText)));
        }
    }
}
