using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class PublishedClient
    {
        public PublishedClient(
            Guid environmentId,
            Guid schemaId,
            Guid clientId,
            Guid clientVersionId,
            IReadOnlyList<Guid> queryIds)
        {
            Id = Guid.NewGuid();
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ClientVersionId = clientVersionId;
            QueryIds = queryIds;
        }

        public PublishedClient(
            Guid id,
            Guid environmentId,
            Guid schemaId,
            Guid clientId,
            Guid clientVersionId,
            IReadOnlyList<Guid> queryIds)
        {
            Id = id;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ClientVersionId = clientVersionId;
            QueryIds = queryIds;
        }

        public Guid Id { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid ClientId { get; }

        public Guid ClientVersionId { get; }

        public IReadOnlyList<Guid> QueryIds { get; }
    }
}
