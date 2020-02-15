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
            IReadOnlyList<DocumentInfo> documents,
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            ExternalId = externalId;
            Type = type;
            Documents = documents;
            Tags = tags;
        }

        public PublishDocumentMessage(
            string sessionId,
            Guid environmentId,
            Guid schemaId,
            string? externalId,
            IReadOnlyList<DocumentInfo> documents,
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ExternalId = externalId;
            Documents = documents;
            Type = DocumentType.Schema;
            Tags = tags;
        }

        public string SessionId { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid? ClientId { get; }

        public string? ExternalId { get; }

        public DocumentType Type { get; }

        public IReadOnlyList<DocumentInfo> Documents { get; }

        public IReadOnlyList<Tag> Tags { get; }
    }
}
