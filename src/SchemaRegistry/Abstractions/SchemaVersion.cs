using System.Collections.Generic;
using System;

namespace MarshmallowPie
{
    public class SchemaVersion
    {
        public SchemaVersion(
            Guid id,
            Guid schemaId,
            DateTime published,
            string sourceText,
            IReadOnlyList<Tag> tags)
        {
            Id = id;
            SchemaId = schemaId;
            Published = published;
            SourceText = sourceText;
            Tags = tags;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public DateTime Published { get; }

        public string SourceText { get; }

        public IReadOnlyList<Tag> Tags { get; }
    }
}
