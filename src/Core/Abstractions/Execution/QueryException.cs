using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotChocolate.Execution
{
    [Serializable]
    public class QueryException
        : GraphQLException
    {
        public QueryException(string message)
            : base(message)
        {
        }

        public QueryException(IError error)
            : base(error)
        {
        }

        public QueryException(params IError[] errors)
            : base(errors)
        {
            
        }

        public QueryException(IEnumerable<IError> errors)
            : base(errors)
        {
        }

        protected QueryException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
