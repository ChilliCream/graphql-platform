using HotChocolate;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.ErrorFilters
{
    internal sealed class DuplicateKeyErrorFilter
        : IErrorFilter
    {
        public IError OnError(IError error)
        {
            if (error.Exception is DuplicateKeyException ex)
            {
                return ErrorBuilder.FromError(error)
                    .SetMessage(ex.Message)
                    .SetException(null)
                    .SetCode(ErrorCodes.DuplicateKey)
                    .Build();
            }
            return error;
        }
    }
}
