using System.Collections.Generic;

namespace MarshmallowPie.GraphQL.Clients
{
    public class PublishClientInput
    {
        public PublishClientInput(
            string environmentName,
            string schemaName,
            string clientName,
            string? externalId,
            QueryFileFormat format,
            IReadOnlyList<QueryFile> files,
            IReadOnlyList<TagInput>? tags,
            string? clientMutationId)
        {
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ClientName = clientName;
            ExternalId = externalId;
            Format = format;
            Files = files;
            Tags = tags;
            ClientMutationId = clientMutationId;
        }

        public string EnvironmentName { get; }

        public string SchemaName { get; }

        public string ClientName { get; }

        public string? ExternalId { get; }

        public QueryFileFormat Format { get; }

        public IReadOnlyList<QueryFile> Files { get; }

        public IReadOnlyList<TagInput>? Tags { get; }

        public string? ClientMutationId { get; }
    }
}
