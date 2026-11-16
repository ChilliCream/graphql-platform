using HotChocolate.Execution.Processing;
using HotChocolate.Text.Json;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    extension(OperationContext context)
    {
        public OperationContext ReportError(
            Exception exception,
            MiddlewareContext resolverContext,
            Path? path = null)
        {
            path ??= resolverContext.Path;

            ArgumentNullException.ThrowIfNull(exception);

            if (exception is GraphQLException ex)
            {
                foreach (var error in ex.Errors)
                {
                    context.ReportError(
                        error.Path is null ? error.WithPath(path) : error,
                        resolverContext);
                }
            }
            else
            {
                var error = ErrorBuilder
                    .FromException(exception)
                    .SetPath(path)
                    .Build();

                context.ReportError(error, resolverContext);
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
            var document = context.Result.Data;

            return context.BuildResult(
                new OperationResultData(
                    document,
                    document.Data.IsNullOrInvalidated,
                    document,
                    document));
        }

        public OperationResult BuildStreamItemsResult()
        {
            var document = context.Result.Data;
            var items = new ResultDocumentItemCollection(document);

            return context.BuildResult(
                new OperationResultData(
                    items,
                    document.Data.IsNullOrInvalidated,
                    items,
                    items));
        }

        private OperationResult BuildResult(OperationResultData data)
        {
            var resultBuilder = context.Result;

            if (!resultBuilder.NonNullViolations.IsEmpty)
            {
                var errorPaths = new HashSet<Path>();

                foreach (var error in resultBuilder.Errors)
                {
                    if (error.Path is not null)
                    {
                        errorPaths.Add(error.Path);
                    }
                }

                if (!errorPaths.IsProperSupersetOf(resultBuilder.NonNullViolations))
                {
                    var errorBuilder = ErrorHelper.NonNullOutputFieldViolation();

                    foreach (var path in resultBuilder.NonNullViolations.Except(errorPaths))
                    {
                        resultBuilder.AddError(errorBuilder.SetPath(path).Build());
                    }
                }
            }

            var result = new OperationResult(
                data,
                resultBuilder.Errors,
                resultBuilder.Extensions)
            {
                RequestIndex = resultBuilder.RequestIndex > -1 ? resultBuilder.RequestIndex : null,
                VariableIndex = resultBuilder.VariableIndex > -1 ? resultBuilder.VariableIndex : null,
                Document = context.Operation.Document,
                ContextData = resultBuilder.ContextData
            };

            // incremental operations deliver several payloads, so their request cleanup is owned by
            // the response stream instead of the payload that happens to be built first.
            if (!context.Operation.HasIncrementalParts)
            {
                foreach (var cleanupTask in resultBuilder.TakeCleanupTasks())
                {
                    result.RegisterForCleanup(cleanupTask);
                }
            }

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
            var nextDepth = depth + 1;

            foreach (var innerError in aggregateError.Errors)
            {
                UnwrapError(errorHandler, innerError, errors, nextDepth);
            }
        }
        else
        {
            errors.Add(handled);
        }
    }
}
