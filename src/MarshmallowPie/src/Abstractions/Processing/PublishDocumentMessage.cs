using System;
using System.Collections.Generic;

namespace MarshmallowPie.Processing
{
    public class PublishDocumentMessage
    {
        public PublishDocumentMessage(
            string sessionId,
            Guid environmentId,
            Guid schemaId,
            Guid clientId,
            string? externalId,
            DocumentType type,
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ExternalId = externalId;
            Type = type;
            Tags = tags;
        }

        public PublishDocumentMessage(
            string sessionId,
            Guid environmentId,
            Guid schemaId,
            string? externalId,
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ExternalId = externalId;
            Type = DocumentType.Schema;
            Tags = tags;
        }

        public string SessionId { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid? ClientId { get; }

        public string? ExternalId { get; }

        public DocumentType Type { get; }

        public IReadOnlyList<Tag> Tags { get; }
    }
}
