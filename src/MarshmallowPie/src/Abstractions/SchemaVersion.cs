using System;
using System.Collections.Generic;
using System.Linq;

namespace MarshmallowPie
{
    public class SchemaVersion
    {
        public SchemaVersion(
            Guid schemaId,
            string sourceText,
            DocumentHash hash,
            IReadOnlyList<Tag> tags,
            DateTime published)
            : this(Guid.NewGuid(), schemaId, sourceText, hash, tags, published)
        {
        }

        public SchemaVersion(
            Guid id,
            Guid schemaId,
            string sourceText,
            DocumentHash hash,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            if (tags.Count > 1)
            {
                tags = new HashSet<Tag>(tags, TagComparer.Default).ToList();
            }

            Id = id;
            SchemaId = schemaId;
            SourceText = sourceText;
            Hash = hash;
            Tags = tags;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public string SourceText { get; }

        public DocumentHash Hash { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }
}
