using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class ClientVersion
    {
        public ClientVersion(
            Guid clientId,
            ISet<Guid> queryIds,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            Id = Guid.NewGuid();
            ClientId = clientId;
            QueryIds = queryIds;
            Tags = tags;
            Published = published;
        }

        public ClientVersion(
            Guid id,
            Guid clientId,
            ISet<Guid> queryIds,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            Id = id;
            ClientId = clientId;
            QueryIds = queryIds;
            Tags = tags;
            Published = published;
        }

        public Guid Id { get; }

        public Guid ClientId { get; }

        public ISet<Guid> QueryIds { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }
}
