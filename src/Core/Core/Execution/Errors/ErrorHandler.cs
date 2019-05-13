using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    public class ErrorHandler
        : IErrorHandler
    {
        private const string _messageProperty = "message";
        private const string _stackTraceProperty = "stackTrace";

        private readonly IErrorFilter[] _filters;
        private readonly bool _includeExceptionDetails;

        public ErrorHandler(
            IEnumerable<IErrorFilter> errorFilters,
            IErrorHandlerOptionsAccessor options)
        {
            if (errorFilters == null)
            {
                throw new ArgumentNullException(nameof(errorFilters));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _filters = errorFilters.ToArray();
            _includeExceptionDetails = options.IncludeExceptionDetails;
        }

        private ErrorHandler()
        {
            _filters = Array.Empty<IErrorFilter>();
            _includeExceptionDetails = false;
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
                current = filter.OnError(current);

                if (current == null)
                {
                    throw new InvalidOperationException(
                        CoreResources.ErrorHandler_ErrorIsNull);
                }
            }

            return current;
        }

        public IErrorBuilder CreateUnexpectedError(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return CreateErrorFromException(exception);
        }

        private IErrorBuilder CreateErrorFromException(Exception exception)
        {
            IErrorBuilder builder = ErrorBuilder.New()
                .SetMessage(CoreResources.ErrorHandler_UnexpectedError)
                .SetException(exception);

            if (_includeExceptionDetails)
            {
                builder
                    .SetExtension(_messageProperty, exception.Message)
                    .SetExtension(_stackTraceProperty, exception.StackTrace);
            }

            return builder;
        }

        public static ErrorHandler Default { get; } =
            new ErrorHandler();
    }
}
