using System;

namespace MarshmallowPie
{
    public class Client
    {
        public Client(
            string name,
            Guid schemaId,
            string? description = null)
        {
            Id = Guid.NewGuid();
            SchemaId = schemaId;
            Name = name;
            Description = description;
        }

        public Client(
            Guid id,
            Guid schemaId,
            string name,
            string? description = null)
        {
            Id = id;
            SchemaId = schemaId;
            Name = name;
            Description = description;
        }

        public Guid Id { get; }

        public Guid SchemaId { get; }

        public string Name { get; }

        public string? Description { get; }
    }
}
