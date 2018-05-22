using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    // TODO : finalize type
    public class QueryException
        : Exception
    {
        public QueryException(QueryError error)
        {

        }

        public QueryException(params QueryError[] errors)
        {

        }

        public QueryException(IEnumerable<QueryError> errors)
        {

        }
    }
}
