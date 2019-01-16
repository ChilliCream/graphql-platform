
using System;
using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class StitchingContext
        : IStitchingContext
    {
        private readonly IDictionary<string, IQueryExecutor> _queryExecutors;

        public StitchingContext(
            IDictionary<string, IQueryExecutor> queryExecutors)
        {
            _queryExecutors = queryExecutors
                ?? throw new ArgumentNullException(nameof(queryExecutors));
        }

        public IQueryExecutor GetQueryExecutor(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name cannot be null or empty.",
                    nameof(schemaName));
            }

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
