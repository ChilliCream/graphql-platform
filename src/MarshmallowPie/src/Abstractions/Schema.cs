using System;

namespace MarshmallowPie
{
#pragma warning disable CA1724
    public class Schema
#pragma warning restore CA1724
    {
        public Schema(
            string name,
            string? description = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
        }

        public Schema(
            Guid id,
            string name,
            string? description = null)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public Guid Id { get; }

        public string Name { get; }

        public string? Description { get; }
    }
}
