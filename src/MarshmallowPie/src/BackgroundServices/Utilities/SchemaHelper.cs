using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;
using Location = HotChocolate.Language.Location;

namespace MarshmallowPie.BackgroundServices
{
    internal static class SchemaHelper
    {
        public static async Task<ISchema?> TryLoadSchemaAsync(
            ISchemaRepository repository,
            IFileStorage storage,
            Guid schemaId,
            Guid environmentId,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                PublishedSchema publishedSchema = await repository.GetPublishedSchemaAsync(
                    schemaId, environmentId, cancellationToken)
                    .ConfigureAwait(false);

                IFileContainer container = await storage.GetContainerAsync(
                    publishedSchema.SchemaVersionId.ToString("N", CultureInfo.InvariantCulture),
                    cancellationToken)
                    .ConfigureAwait(false);

                IEnumerable<IFile> files = await container.GetFilesAsync(
                    cancellationToken)
                    .ConfigureAwait(false);

                DocumentNode? document = await DocumentHelper.TryParseDocumentAsync(
                    files.Single(), logger, cancellationToken)
                    .ConfigureAwait(false);

                if (document is { })
                {
                    return await TryCreateSchemaAsync(
                        document, logger, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await logger.LogIssueAsync(
                    new Issue(
                        "SCHEMA_ERROR",
                        ex.Message,
                        "schema.graphql",
                        new Location(0, 0, 1, 1),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            return null;
        }

        public static async Task<ISchema?> TryCreateSchemaAsync(
            DocumentNode document,
            IssueLogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                // TODO : add custom scalar support => we need to be able to
                // configure the supported literals for that.
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
                            "schema.graphql",
                            error.SyntaxNodes.FirstOrDefault()?.Location
                                ?? new Location(0, 0, 1, 1),
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
                        "schema.graphql",
                        new Location(0, 0, 1, 1),
                        IssueType.Error,
                        ResolutionType.CannotBeFixed),
                    cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }
        }
    }
}
