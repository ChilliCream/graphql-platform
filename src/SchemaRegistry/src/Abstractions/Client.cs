using System;

namespace MarshmallowPie
{
    public class Client
    {
        public Client(
            string name,
            string? description = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
        }

        public Client(
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
