using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class Query
    {
        public Query(
            DocumentHash hash,
            ISet<DocumentHash> externalHashes,
            DateTime published)
        {
            Id = Guid.NewGuid();
            Hash = hash;
            ExternalHashes = externalHashes;
            Published = published;
        }

        public Query(
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
