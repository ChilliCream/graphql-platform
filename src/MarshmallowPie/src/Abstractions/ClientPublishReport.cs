using System;
using System.Collections.Generic;
using System.Globalization;

namespace MarshmallowPie
{
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
                Guid.NewGuid(), clientVersionId, environmentId,
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
            SchemaVersionId = clientVersionId;
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
