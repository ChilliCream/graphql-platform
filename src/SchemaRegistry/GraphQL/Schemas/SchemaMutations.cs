using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
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
            CancellationToken cancellationToken)
        {
            Schema schema = await schemaRepository.GetSchemaAsync(
                input.SchemaName, cancellationToken)
                .ConfigureAwait(false);

            SchemaVersion schemaVersion;

            if (input.Hash is null)
            {
                if (input.SourceText is null)
                {
                    throw new GraphQLException(
                        "The schema hash or the schem source text have to be provided.");
                }

                using var sha = SHA256.Create();
                string hash = Convert.ToBase64String(sha.ComputeHash(
                    Encoding.UTF8.GetBytes(input.SourceText)));

                schemaVersion = new SchemaVersion(
                    schema.Id,
                    input.SourceText,
                    hash,
                    input.Tags ?? Array.Empty<Tag>(),
                    DateTime.UtcNow);

                await schemaRepository.AddSchemaVersionAsync(
                    schemaVersion, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                schemaVersion = await schemaRepository.GetSchemaVersionAsync(
                    input.Hash, cancellationToken)
                    .ConfigureAwait(false);

                if (input.Tags is { })
                {
                    var tags = new List<Tag>(schemaVersion.Tags);
                    tags.AddRange(input.Tags);
                    schemaVersion = new SchemaVersion(
                        schemaVersion.Id,
                        schemaVersion.SourceText,
                        schemaVersion.Hash,
                        tags,
                        schemaVersion.Published);

                    await schemaRepository.UpdateSchemaVersionAsync(
                        schemaVersion, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            Environment environment = await environmentRepository.GetEnvironmentAsync(
                input.EnvironmentName, cancellationToken)
                .ConfigureAwait(false);

            SchemaPublishReport? report = await schemaRepository.GetPublishReportAsync(
                schemaVersion.Id, environment.Id, cancellationToken)
                .ConfigureAwait(false);

            if (report is null)
            {
                report = new SchemaPublishReport(
                    schemaVersion.SchemaId,
                    environment.Id,
                    Array.Empty<Issue>(),
                    PublishState.Published,
                    DateTime.UtcNow);

                await schemaRepository.AddPublishReportAsync(
                    report, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new PublishSchemaPayload(schemaVersion, report, input.ClientMutationId);
        }
    }
}
