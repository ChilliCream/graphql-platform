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
                return error.WithMessage(ex.Message).WithException(null);
            }
            return error;
        }
    }
}
