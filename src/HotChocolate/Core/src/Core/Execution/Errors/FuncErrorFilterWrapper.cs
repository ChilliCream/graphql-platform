using System;

namespace HotChocolate.Execution
{
    internal class FuncErrorFilterWrapper
        : IErrorFilter
    {
        private readonly Func<IError, IError> _errorFilter;

        public FuncErrorFilterWrapper(
            Func<IError, IError> errorFilter)
        {
            _errorFilter = errorFilter
                ?? throw new ArgumentNullException(nameof(errorFilter));
        }

        public IError OnError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return _errorFilter(error);
        }
    }
}
