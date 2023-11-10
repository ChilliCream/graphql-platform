using System.Collections.Generic;

namespace HotChocolate.Execution;

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
}
