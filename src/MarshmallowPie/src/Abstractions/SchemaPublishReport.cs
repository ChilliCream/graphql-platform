using System;
using System.Collections.Generic;
using System.Globalization;

namespace MarshmallowPie
{
    public class SchemaPublishReport
    {
        public SchemaPublishReport(

            Guid schemaVersionId,
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

        public SchemaPublishReport(
            Guid id,
            Guid schemaVersionId,
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
