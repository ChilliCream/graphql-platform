using System;

namespace MarshmallowPie
{
    public class Environment
    {
        public Environment(
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
}
