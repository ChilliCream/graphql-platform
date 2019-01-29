using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public interface IErrorHandler
    {
        IError Handle(IError error);
        IError Handle(Exception exception, Action<IErrorBuilder> configure);
    }

    public static class ErrorHandlerExtensions
    {
        public static IError Handle(
            this IErrorHandler errorHandler,
            Exception exception)
        {
            if (errorHandler == null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return errorHandler.Handle(exception, b => { });
        }

        public static IEnumerable<IError> Handle(
            this IErrorHandler errorHandler,
            IEnumerable<IError> errors)
        {
            if (errorHandler == null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            return HandleEnumerator(errorHandler, errors);
        }

        private static IEnumerable<IError> HandleEnumerator(
            IErrorHandler errorHandler,
            IEnumerable<IError> errors)
        {
            foreach (IError error in errors)
            {
                yield return errorHandler.Handle(error);
            }
        }
    }
}
