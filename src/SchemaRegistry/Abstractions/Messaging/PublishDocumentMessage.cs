using System;
using System.Collections.Generic;

namespace MarshmallowPie.Messaging
{
    public class PublishDocumentMessage
    {
        public string SessionId { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaId { get; }

        public Guid? ClientId { get; }

        public DocumentType Type { get; }

        public IReadOnlyList<Tag> Tags { get; }
    }
}
