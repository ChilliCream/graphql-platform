using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class SchemaPublishReport
    {
        public SchemaPublishReport(

            Guid schemaVersionId,
            Guid environmentId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
            : this(Guid.NewGuid(), schemaVersionId, environmentId, issues, state, published)
        {
        }

        public SchemaPublishReport(
            Guid id,
            Guid schemaVersionId,
            Guid environmentId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
        {
            Id = id;
            SchemaVersionId = schemaVersionId;
            EnvironmentId = environmentId;
            Issues = issues;
            State = state;
            Published = published;
        }

        public Guid Id { get; }

        public Guid SchemaVersionId { get; }

        public Guid EnvironmentId { get; }

        public IReadOnlyList<Issue> Issues { get; }

        public PublishState State { get; }

        public DateTime Published { get; }
    }
}
