using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using MarshmallowPie.Processing;
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

        public static async Task<DocumentNode?> TryParseDocumentAsync(
            string fileName,
            string sourceText,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                return Utf8GraphQLParser.Parse(sourceText);
            }
            catch (SyntaxException ex)
            {
                await logger.LogIssueAsync(
                    new Issue(
                        "SYNTAX_ERROR",
                        ex.Message,
                        fileName,
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
                        fileName,
                        new Location(0, 0, 0, 0),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
        }

        public static async Task<RelayDocument?> TryParseRelayDocumentAsync(
            IFile file,
            DocumentInfo documentInfo,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                using var fs = await file.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var sr = new StreamReader(fs);

                string sourceText = await sr.ReadToEndAsync().ConfigureAwait(false);

                return RelayDocument.Parse(documentInfo, sourceText);
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
            if (document is { })
            {
                if (document.Definitions.OfType<IExecutableDefinitionNode>().Any())
                {
                    return QuerySyntaxSerializer.Serialize(document, true);
                }
                else
                {
                    return SchemaSyntaxSerializer.Serialize(document, true);
                }
            }

            return await file.ReadAllTextAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
