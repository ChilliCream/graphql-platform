using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "Mutation")]
    public class SchemaMutations
    {
        public async Task<CreateSchemaPayload> CreateSchema(
            CreateSchemaInput input,
            [Service]ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            var schema = new Schema(input.Name, input.Description);

            await repository.AddSchemaAsync(schema, cancellationToken).ConfigureAwait(false);

            return new CreateSchemaPayload(schema, input.ClientMutationId);
        }

        public async Task<UpdateSchemaPayload> UpdateSchema(
            UpdateSchemaInput input,
            [Service]IIdSerializer idSerializer,
            [Service]ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(input.Id);

            if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            var schema = new Schema(
                (Guid)deserializedId.Value,
                input.Name,
                input.Description);

            await repository.UpdateSchemaAsync(schema, cancellationToken)
                .ConfigureAwait(false);

            return new UpdateSchemaPayload(schema, input.ClientMutationId);
        }

        public async Task<PublishSchemaPayload> PublishSchemaAsync(
            PublishSchemaInput input,
            [Service]ISchemaRepository schemaRepository,
            [Service]IEnvironmentRepository environmentRepository,
            [DataLoader]SchemaByNameDataLoader schemaDataLoader,
            [DataLoader]EnvironmentByNameDataLoader environmentDataLoader,
            CancellationToken cancellationToken)
        {
            Schema schema = await schemaDataLoader.LoadAsync(
                input.SchemaName, cancellationToken)
                .ConfigureAwait(false);

            Environment environment = await environmentDataLoader.LoadAsync(
                input.EnvironmentName, cancellationToken)
                .ConfigureAwait(false);

            SchemaVersion? schemaVersion;

            if (input.Hash is null)
            {
                if (input.SourceText is null)
                {
                    throw new GraphQLException(
                        "The schema hash or the schem source text have to be provided.");
                }

                schemaVersion = await CreateSchemaVersionAsync(
                    input.SourceText,
                    input.Tags ?? Array.Empty<TagInput>(),
                    schema,
                    schemaRepository,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                schemaVersion = await UpdateSchemaVersionAsync(
                    input.Hash,
                    input.Tags ?? Array.Empty<TagInput>(),
                    schemaRepository,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            SchemaPublishReport report = await TryCreateReportAsync(
                schema.Id,
                schemaVersion.Id,
                environment.Id,
                schemaRepository,
                cancellationToken)
                .ConfigureAwait(false);

            return new PublishSchemaPayload(schemaVersion, report, input.ClientMutationId);
        }

        private async Task<SchemaVersion> CreateSchemaVersionAsync(
            string sourceText,
            IReadOnlyList<TagInput> tags,
            Schema schema,
            ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            using var sha = SHA256.Create();
            string hash = Convert.ToBase64String(sha.ComputeHash(
                Encoding.UTF8.GetBytes(sourceText)));

            var schemaVersion = new SchemaVersion(
                schema.Id,
                sourceText,
                hash,
                tags.Select(t => new Tag(t.Key, t.Value, DateTime.UtcNow)).ToList(),
                DateTime.UtcNow);

            await repository.AddSchemaVersionAsync(
                schemaVersion, cancellationToken)
                .ConfigureAwait(false);

            return schemaVersion;
        }

        private async Task<SchemaVersion> UpdateSchemaVersionAsync(
            string hash,
            IReadOnlyList<TagInput> tags,
            ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            SchemaVersion? schemaVersion =
                await repository.GetSchemaVersionAsync(
                    hash, cancellationToken)
                    .ConfigureAwait(false);

            if (schemaVersion is null)
            {
                throw new GraphQLException(
                    "The specified schema hash is invalid.");
            }

            if (tags is { })
            {
                var list = new List<Tag>(schemaVersion.Tags);
                list.AddRange(tags.Select(t => new Tag(
                    t.Key, t.Value, DateTime.UtcNow)));

                schemaVersion = new SchemaVersion(
                    schemaVersion.Id,
                    schemaVersion.SourceText,
                    schemaVersion.Hash,
                    list,
                    schemaVersion.Published);

                await repository.UpdateSchemaVersionAsync(
                    schemaVersion, cancellationToken)
                    .ConfigureAwait(false);
            }

            return schemaVersion;
        }

        private async Task<SchemaPublishReport> TryCreateReportAsync(
            Guid schemaId,
            Guid schemaVersionId,
            Guid environmentId,
            ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            SchemaPublishReport? report =
                await repository.GetPublishReportAsync(
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

                await repository.AddPublishReportAsync(
                    report, cancellationToken)
                    .ConfigureAwait(false);
            }

            return report;
        }
    }
}
