using System.Collections.Generic;
using System;

namespace MarshmallowPie
{
    public class Schema
    {
        public Schema(
            string name,
            string? description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public Schema(
            Guid id,
            string name,
            string? description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public Guid Id { get; }

        public string Name { get; }

        public string? Description { get; }
    }


    public class SchemaVersion
    {
        public Guid Id { get; }

        public Guid SchemaId { get; }

        public DateTime Published { get; }

        public string SourceText { get; }

        public IReadOnlyList<Tag> Tags { get; }

    }

    public class Tag
    {
        public string Key { get; }

        public string Value { get; }

        public DateTime Published { get; }
    }

    public class SchemaVersionPublishReport
    {
        public Guid Id { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaVersionId { get; }

        public DateTime Published { get; }

        public PublishState State { get; }
    }

    public enum PublishState
    {
        Rejected,
        Published
    }
}
