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

        public Guid SchemaId { get; }

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

    public class ClientPublishReport
    {
        public ClientPublishReport(

            Guid clientVersionId,
            Guid environmentId,
            string? externalId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
            : this(
                Guid.NewGuid(), schemaVersionId, environmentId,
                externalId, issues, state, published)
        {
        }

        public ClientPublishReport(
            Guid id,
            Guid clientVersionId,
            Guid environmentId,
            string? externalId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
        {
            Id = id;
            SchemaVersionId = schemaVersionId;
            EnvironmentId = environmentId;
            ExternalId = externalId ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            Issues = issues;
            State = state;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaVersionId { get; }

        public Guid EnvironmentId { get; }

        public string ExternalId { get; }

        public IReadOnlyList<Issue> Issues { get; }

        public PublishState State { get; }

        public DateTime Published { get; }
    }
}
