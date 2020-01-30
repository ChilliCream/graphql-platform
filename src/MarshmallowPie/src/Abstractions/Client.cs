using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    public class ClientVersion
    {
        public Guid Id { get; }

        public Guid ClientId { get; }

        public ISet<Guid> QueryIds { get; }

        public IReadOnlyList<Tag> Tags { get; }

        public DateTime Published { get; }
    }

    public class Query
    {
        public Guid Id { get; }

        public ISet<DocumentHash> ExternalHashes { get; }

        public DocumentHash Hash { get; }

        public DateTime Published { get; }
    }

}
