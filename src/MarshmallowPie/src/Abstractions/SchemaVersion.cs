using System;
using System.Collections.Generic;
using System.Globalization;

namespace MarshmallowPie
{
    public class SchemaVersion
    {
        public SchemaVersion(
            Guid schemaId,
            string? externalId,
            DocumentHash hash,
            IReadOnlyList<Tag> tags,
            DateTime published)
            : this(Guid.NewGuid(), schemaId, externalId, hash, tags, published)
        {
        }

        public SchemaVersion(
            Guid id,
            Guid schemaId,
            string? externalId,
            DocumentHash hash,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            Id = id;
            SchemaId = schemaId;
            ExternalId = externalId ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            Hash = hash;
            Tags = tags;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public string ExternalId { get; }

        public DocumentHash Hash { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }
}
