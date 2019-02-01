using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class StitchingContext
        : IStitchingContext
    {
        private readonly IDictionary<string, IQueryExecutor> _queryExecutors;

        public StitchingContext(IEnumerable<IRemoteExecutorAccessor> executors)
        {
            if (executors == null)
            {
                throw new ArgumentNullException(nameof(executors));
            }

            _queryExecutors = executors.ToDictionary(
                t => t.SchemaName,
                t => t.Executor);
        }

        public StitchingContext(
            IDictionary<string, IQueryExecutor> queryExecutors)
        {
            _queryExecutors = queryExecutors
                ?? throw new ArgumentNullException(nameof(queryExecutors));
        }

        public IRemoteQueryClient GetRemoteQueryClient(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name cannot be null or empty.",
                    nameof(schemaName));
            }
            return new RemoteQueryClient(GetQueryExecutor(schemaName));
        }


        public IQueryExecutor GetQueryExecutor(string schemaName)
        {
            if (_queryExecutors.TryGetValue(
                    schemaName,
                    out IQueryExecutor executor))
            {
                return executor;
            }

            throw new ArgumentException(
                $"There is now shema with the given name `{schemaName}`.");
        }
    }
}
