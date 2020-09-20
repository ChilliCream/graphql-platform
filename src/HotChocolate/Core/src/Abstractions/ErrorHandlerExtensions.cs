using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate
{
    public static class ErrorHandlerExtensions
    {
        public static IReadOnlyList<IError> Handle(
            this IErrorHandler errorHandler,
            IEnumerable<IError> errors)
        {
            if (errorHandler is null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }

            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            return HandleEnumerator(errorHandler, errors).ToList();
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
