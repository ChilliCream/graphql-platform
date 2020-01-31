using System;

namespace MarshmallowPie
{
    public class PublishedClient
    {
        public PublishedClient(
            Guid environmentId,
            Guid schemaId,
            Guid clientId,
            Guid clientVersionId)
        {
            Id = Guid.NewGuid();
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ClientVersionId = clientVersionId;
        }

        public PublishedClient(
            Guid id,
            Guid environmentId,
            Guid schemaId,
            Guid clientId,
            Guid clientVersionId)
        {
            Id = id;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ClientVersionId = clientVersionId;
        }

        public Guid Id { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid ClientId { get; }

        public Guid ClientVersionId { get; }
    }
}
