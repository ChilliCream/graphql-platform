using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public interface IErrorHandler
    {
        IError Handle(IError error);

        /// <summary>
        /// Creates an error from an unexpected exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        IErrorBuilder CreateUnexpectedError(Exception exception);
    }

    public static class ErrorHandlerExtensions
    {
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
