using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Execution
{
    public class ErrorHandler
        : IErrorHandler
    {
        private const string _messageProperty = "message";
        private const string _stackTraceProperty = "stackTrace";

        private readonly IErrorFilter[] _filters;
        private readonly bool _includeExceptionDetails;
        private readonly ILogger<ErrorHandler> _logger;

        public ErrorHandler(
            IEnumerable<IErrorFilter> errorFilters,
            IErrorHandlerOptionsAccessor options,
            ILogger<ErrorHandler> logger = null)
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
            _logger = logger ?? new NullLogger<ErrorHandler>();
        }

        private ErrorHandler()
        {
            _filters = Array.Empty<IErrorFilter>();
            _includeExceptionDetails = false;
            _logger = new NullLogger<ErrorHandler>();
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

            _logger.LogError(exception, exception.Message);
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
