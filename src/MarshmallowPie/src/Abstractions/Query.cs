using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class QueryDocument
    {
        public QueryDocument(DocumentHash hash)
        {
            Id = Guid.NewGuid();
            Hash = hash;
            ExternalHashes = new HashSet<DocumentHash>();
            Published = DateTime.UtcNow;
        }

        public QueryDocument(
            DocumentHash hash,
            ISet<DocumentHash> externalHashes,
            DateTime published)
        {
            Id = Guid.NewGuid();
            Hash = hash;
            ExternalHashes = externalHashes;
            Published = published;
        }

        public QueryDocument(
            Guid id,
            DocumentHash hash,
            ISet<DocumentHash> externalHashes,
            DateTime published)
        {
            Id = id;
            Hash = hash;
            ExternalHashes = externalHashes;
            Published = published;
        }

        public Guid Id { get; }

        public DocumentHash Hash { get; }

        public ISet<DocumentHash> ExternalHashes { get; }

        public DateTime Published { get; }
    }
}
