using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using MarshmallowPie.Storage;
using Location = HotChocolate.Language.Location;

namespace MarshmallowPie.BackgroundServices
{
    internal static class DocumentHelper
    {
        public static async Task<DocumentNode?> TryParseDocumentAsync(
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
                    new Issue(
                        "SYNTAX_ERROR",
                        ex.Message,
                        file.Name,
                        new Location(ex.Position, ex.Position, ex.Line, ex.Column),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    new Issue(
                        "PARSING_FAILED",
                        ex.Message,
                        file.Name,
                        new Location(0, 0, 0, 0),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
        }

        public static async Task<string> LoadSourceTextAsync(
            IFile file,
            DocumentNode? document,
            CancellationToken cancellationToken)
        {
            return document is { }
                ? SchemaSyntaxSerializer.Serialize(document, true)
                : await ReadSchemaSourceTextAsync(
                    file, cancellationToken)
                    .ConfigureAwait(false);
        }

        private static async Task<string> ReadSchemaSourceTextAsync(
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
    }
}
