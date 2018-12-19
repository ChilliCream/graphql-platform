
using System;
using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class StitchingContext
        : IStitchingContext
    {
        private readonly IDictionary<string, IQueryExecuter> _queryExecuters;

        public StitchingContext(
            IDictionary<string, IQueryExecuter> queryExecuters)
        {
            _queryExecuters = queryExecuters
                ?? throw new ArgumentNullException(nameof(queryExecuters));
        }

        public IQueryExecuter GetQueryExecuter(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name cannot be null or empty.",
                    nameof(schemaName));
            }

            if (_queryExecuters.TryGetValue(
                    schemaName,
                    out IQueryExecuter executer))
            {
                return executer;
            }

            throw new ArgumentException(
                $"There is now shema with the given name `{schemaName}`.");
        }
    }
}
