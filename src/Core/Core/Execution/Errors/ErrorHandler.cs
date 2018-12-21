using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;

namespace HotChocolate.Execution
{
    public class ErrorHandler
        : IErrorHandler
    {
        private readonly IErrorFilter[] _filters;
        // TODO : add error options
        private readonly bool _includeExceptionDetails;

        public ErrorHandler(IEnumerable<IErrorFilter> errorFilters)
        {
            _filters = errorFilters?.ToArray() ?? Array.Empty<IErrorFilter>();
        }

        public IError Handle(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            IError current = error;

            foreach (IErrorFilter filter in _filters)
            {
                current = filter.OnError(current, null);

                if (current == null)
                {
                    throw new InvalidOperationException(
                        "IErrorFilter.OnError mustn't return null.");
                }
            }

            return current;
        }

        public IEnumerable<IError> Handle(IEnumerable<IError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            foreach (IError error in errors)
            {
                yield return Handle(error);
            }
        }

        public IError Handle(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            IError current = CreateErrorFromException(exception);

            foreach (IErrorFilter filter in _filters)
            {
                current = filter.OnError(current, exception);

                if (current == null)
                {
                    throw new InvalidOperationException(
                        "IErrorFilter.OnError mustn't return null.");
                }
            }

            return current;
        }

        private IError CreateErrorFromException(Exception exception)
        {
            if (_includeExceptionDetails)
            {
                return new QueryError("Unexpected Execution Error",
                    new ErrorProperty("message", exception.Message),
                    new ErrorProperty("stackTrace", exception.StackTrace));
            }
            else
            {
                return new QueryError("Unexpected Execution Error");
            }
        }

        public static ErrorHandler Default { get; } = new ErrorHandler(null);
    }
}
