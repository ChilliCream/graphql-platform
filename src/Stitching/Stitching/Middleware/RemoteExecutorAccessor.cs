
using System;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class RemoteExecutorAccessor
        : IRemoteExecutorAccessor
    {
        public RemoteExecutorAccessor(
            string schemaName,
            IQueryExecutor executor)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            this.SchemaName = schemaName;
            this.Executor = executor
                ?? throw new ArgumentNullException(nameof(executor));

        }
        public string SchemaName { get; }

        public IQueryExecutor Executor { get; }
    }
}
