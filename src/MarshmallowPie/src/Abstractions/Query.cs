using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class QueryDocument
    {
        public QueryDocument(Guid schemaId, DocumentHash hash)
        {
            Id = Guid.NewGuid();
            SchemaId = schemaId;
            Hash = hash;
            ExternalHashes = new HashSet<DocumentHash>();
            Published = DateTime.UtcNow;
        }

        public QueryDocument(
            Guid schemaId,
            DocumentHash hash,
            ISet<DocumentHash> externalHashes,
            DateTime published)
        {
            Id = Guid.NewGuid();
            SchemaId = schemaId;
            Hash = hash;
            ExternalHashes = externalHashes;
            Published = published;
        }

        public QueryDocument(
            Guid id,
            Guid schemaId,
            DocumentHash hash,
            ISet<DocumentHash> externalHashes,
            DateTime published)
        {
            Id = id;
            SchemaId = schemaId;
            Hash = hash;
            ExternalHashes = externalHashes;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public DocumentHash Hash { get; }

        public ISet<DocumentHash> ExternalHashes { get; }

        public DateTime Published { get; }
    }
}
