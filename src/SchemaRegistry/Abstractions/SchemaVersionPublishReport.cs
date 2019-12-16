using System;

namespace MarshmallowPie
{
    public class SchemaVersionPublishReport
    {
        public Guid Id { get; }

        public Guid EnvironmentId { get; }

        public Guid SchemaVersionId { get; }

        public DateTime Published { get; }

        public PublishState State { get; }
    }
}
