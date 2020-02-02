using System;

namespace MarshmallowPie
{
    public class PublishedSchema
    {
        public PublishedSchema(
            Guid environmentId,
            Guid schemaId,
            Guid schemaVersionId)
        {
            Id = Guid.NewGuid();
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            SchemaVersionId = schemaVersionId;
        }

        public PublishedSchema(
            Guid id,
            Guid environmentId,
            Guid schemaId,
            Guid schemaVersionId)
        {
            Id = id;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            SchemaVersionId = schemaVersionId;
        }

        public Guid Id { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid SchemaVersionId { get; }
    }
}
