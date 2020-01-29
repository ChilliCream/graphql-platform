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
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            ClientId = clientId;
            Type = DocumentType.Query;
            Tags = tags;
        }

        public PublishDocumentMessage(
            string sessionId,
            Guid environmentId,
            Guid schemaId,
            IReadOnlyList<Tag> tags)
        {
            SessionId = sessionId;
            EnvironmentId = environmentId;
            SchemaId = schemaId;
            Type = DocumentType.Schema;
            Tags = tags;
        }

        public string SessionId { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid? ClientId { get; }

        public DocumentType Type { get; }

        public IReadOnlyList<Tag> Tags { get; }
    }
}
