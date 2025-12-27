using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    extension(OperationContext context)
    {
        public OperationContext ReportError(
            Exception exception,
            MiddlewareContext resolverContext,
            ISelection? selection = null,
            Path? path = null)
        {
            selection ??= resolverContext.Selection;
            path ??= resolverContext.Path;

            ArgumentNullException.ThrowIfNull(exception);

            if (exception is GraphQLException ex)
            {
                foreach (var error in ex.Errors)
                {
                    ReportError(context, error, resolverContext);
                }
            }
            else
            {
                var error = ErrorBuilder
                    .FromException(exception)
                    .SetPath(path)
                    .AddLocations(selection.GetSyntaxNodes())
                    .Build();

                ReportError(context, error, resolverContext);
            }

            return context;
        }

        public OperationContext ReportError(
            IError error,
            MiddlewareContext resolverContext)
        {
            var errors = new List<IError>();

            UnwrapError(
                context.ErrorHandler,
                error,
                errors);

            context.Result.AddErrorRange(errors);

            foreach (var handled in errors)
            {
                context.DiagnosticEvents.ResolverError(resolverContext, handled);
            }

            return context;
        }

        public OperationResult BuildResult()
        {
            var resultBuilder = context.Result;

            var result = new OperationResult(
                new OperationResultData(
                    resultBuilder.Data,
                    resultBuilder.Data.Data.IsNullOrInvalidated,
                    resultBuilder.Data,
                    resultBuilder.Data),
                resultBuilder.Errors,
                resultBuilder.Extensions)
            {
                RequestIndex = resultBuilder.RequestIndex > -1 ? resultBuilder.RequestIndex : 0,
                VariableIndex = resultBuilder.VariableIndex > -1 ? resultBuilder.VariableIndex : 0,
                ContextData = resultBuilder.ContextData
            };

            if (resultBuilder.Path is not null
                || resultBuilder.HasNext.HasValue
                || !resultBuilder.Pending.IsEmpty
                || !resultBuilder.Incremental.IsEmpty
                || !resultBuilder.Completed.IsEmpty)
            {
                result.Features.Set(
                    new IncrementalDataFeature
                    {
                        Path = resultBuilder.Path,
                        HasNext = resultBuilder.HasNext,
                        Pending = resultBuilder.Pending,
                        Incremental = resultBuilder.Incremental,
                        Completed = resultBuilder.Completed
                    });
            }

            return result;
        }
    }

    private static void UnwrapError(
        IErrorHandler errorHandler,
        IError error,
        List<IError> errors,
        int depth = 0)
    {
        if (depth > 4)
        {
            throw new InvalidOperationException(
                "Error reporting depth exceeded. "
                + "Aggregate error are not allowed to be nested beyond 4 levels.");
        }

        var handled = errorHandler.Handle(error);

        if (handled is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                UnwrapError(errorHandler, innerError, errors, depth++);
            }
        }
        else
        {
            errors.Add(error);
        }
    }
}
