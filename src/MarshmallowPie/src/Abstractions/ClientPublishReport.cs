using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public class ClientPublishReport
    {
        public ClientPublishReport(
            Guid clientVersionId,
            Guid environmentId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
            : this(
                Guid.NewGuid(), clientVersionId, environmentId,
                issues, state, published)
        {
        }

        public ClientPublishReport(
            Guid id,
            Guid clientVersionId,
            Guid environmentId,
            IReadOnlyList<Issue> issues,
            PublishState state,
            DateTime published)
        {
            Id = id;
            ClientVersionId = clientVersionId;
            EnvironmentId = environmentId;
            Issues = issues;
            State = state;
            Published = published;
        }

        public Guid Id { get; }

        public Guid ClientVersionId { get; }

        public Guid EnvironmentId { get; }

        public IReadOnlyList<Issue> Issues { get; }

        public PublishState State { get; }

        public DateTime Published { get; }
    }
}
