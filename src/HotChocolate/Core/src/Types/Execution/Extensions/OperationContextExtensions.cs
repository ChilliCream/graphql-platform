using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    extension(OperationContext context)
    {
        public OperationContext ReportError(Exception exception,
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
                    ReportError(context, error, resolverContext, selection);
                }
            }
            else
            {
                var error = ErrorBuilder
                    .FromException(exception)
                    .SetPath(path)
                    .AddLocations(selection.GetSyntaxNodes())
                    .Build();

                ReportError(context, error, resolverContext, selection);
            }

            return context;
        }

        public OperationContext ReportError(IError error,
            MiddlewareContext resolverContext,
            ISelection? selection = null)
        {
            var errors = new List<IError>();

            ReportSingleError(
                context.ErrorHandler,
                error,
                errors);

            selection ??= resolverContext.Selection;

            foreach (var handled in errors)
            {
                context.Result.AddError(handled, selection);
                context.DiagnosticEvents.ResolverError(resolverContext, handled);
            }

            return context;
        }

        public OperationContext SetLabel(string? label)
        {
            context.Result.SetLabel(label);
            return context;
        }

        public OperationContext SetPath(Path? path)
        {
            context.Result.SetPath(path);
            return context;
        }

        public OperationContext SetData(ObjectResult objectResult)
        {
            context.Result.SetData(objectResult);
            return context;
        }

        public OperationContext SetItems(IReadOnlyList<object?> items)
        {
            context.Result.SetItems(items);
            return context;
        }

        public OperationContext SetPatchId(uint patchId)
        {
            context.Result.SetContextData(WellKnownContextData.PatchId, patchId);
            return context;
        }

        public OperationContext ClearResult()
        {
            context.Result.Clear();
            return context;
        }

        public IOperationResult BuildResult()
            => context.Result.BuildResult();
    }

    private static void ReportSingleError(
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
                ReportSingleError(
                    errorHandler,
                    innerError,
                    errors,
                    depth++);
            }
        }
        else
        {
            errors.Add(error);
        }
    }
}
