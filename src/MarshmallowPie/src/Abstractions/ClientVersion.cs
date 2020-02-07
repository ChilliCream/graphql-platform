using System;
using System.Collections.Generic;
using System.Globalization;

namespace MarshmallowPie
{
    public class ClientVersion
    {
        public ClientVersion(
            Guid clientId,
            string? externalId,
            IReadOnlyList<Guid> queryIds,
            IReadOnlyList<Tag> tags,
            DateTime published)
            : this(Guid.NewGuid(), clientId, externalId, queryIds, tags, published)
        {
        }

        public ClientVersion(
            Guid id,
            Guid clientId,
            string? externalId,
            IReadOnlyList<Guid> queryIds,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            Id = id;
            ClientId = clientId;
            ExternalId = externalId ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            QueryIds = queryIds;
            Tags = tags;
            Published = published;
        }

        public Guid Id { get; }

        public Guid ClientId { get; }

        public string ExternalId { get; }

        public IReadOnlyList<Guid> QueryIds { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }
}
