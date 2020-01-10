using System;

namespace MarshmallowPie
{
    public class Environment
    {
        public Environment(
            string name,
            string? description = null)
            : this(Guid.NewGuid(), name, description)
        {
        }

        public Environment(
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
