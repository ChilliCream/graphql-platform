using System;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteExecutorAccessor
        : IRemoteExecutorAccessor
    {
        public RemoteExecutorAccessor(
            NameString schemaName,
            IQueryExecutor executor)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
                    nameof(schemaName));
            }

            this.SchemaName = schemaName;
            this.Executor = executor
                ?? throw new ArgumentNullException(nameof(executor));

        }

        public NameString SchemaName { get; }

        public IQueryExecutor Executor { get; }
    }
}
