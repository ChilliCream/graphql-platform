using System;

namespace HotChocolate.Execution
{
    internal class FuncErrorFilterWrapper
        : IErrorFilter
    {
        private readonly Func<IError, Exception, IError> _errorFilter;

        public FuncErrorFilterWrapper(
            Func<IError, Exception, IError> errorFilter)
        {
            _errorFilter = errorFilter
                ?? throw new ArgumentNullException(nameof(errorFilter));
        }

        public IError OnError(IError error, Exception exception)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return _errorFilter(error, exception);
        }
    }
}
