using System.Diagnostics.Tracing;
using System.Linq;
using System.Collections.Generic;
using System;

namespace MarshmallowPie
{
    public class SchemaVersion
    {
        public SchemaVersion(
            Guid schemaId,
            string sourceText,
            IReadOnlyList<Tag> tags,
            DateTime published)
            : this(Guid.NewGuid(), schemaId, sourceText, tags, published)
        {
        }

        public SchemaVersion(
            Guid id,
            Guid schemaId,
            string sourceText,
            IReadOnlyList<Tag> tags,
            DateTime published)
        {
            if (tags.Count > 1)
            {
                var keys = new HashSet<string>(tags.Select(t => t.Key));
                if (keys.Count < tags.Count)
                {
                    throw new ArgumentException("The tag keys have to be unique.", nameof(tags));
                }
            }

            Id = id;
            SchemaId = schemaId;
            SourceText = sourceText;
            Tags = tags;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public string SourceText { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }
}
