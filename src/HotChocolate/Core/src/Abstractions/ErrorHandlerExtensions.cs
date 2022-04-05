using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate;

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

        var result = new List<IError>();

        foreach (IError error in errors)
        {
            if (error is AggregateError aggregateError)
            {
                foreach (IError? innerError in aggregateError.Errors)
                {
                    AddProcessed(errorHandler.Handle(innerError));
                }
            }
            else
            {
                AddProcessed(errorHandler.Handle(error));
            }
        }

        return result;

        void AddProcessed(IError error)
        {
            if (error is AggregateError aggregateError)
            {
                foreach (IError? innerError in aggregateError.Errors)
                {
                    result.Add(innerError);
                }
            }
            else
            {
                result.Add(error);
            }
        }
    }
}
