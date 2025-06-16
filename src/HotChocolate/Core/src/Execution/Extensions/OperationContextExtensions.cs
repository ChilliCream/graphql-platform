using System.Diagnostics;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    public static OperationContext ReportError(
        this OperationContext operationContext,
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
                ReportError(operationContext, error, resolverContext, selection);
            }
        }
        else
        {
            var error = ErrorBuilder
                .FromException(exception)
                .SetPath(path)
                .AddLocation(selection.SyntaxNode)
                .Build();

            ReportError(operationContext, error, resolverContext, selection);
        }

        return operationContext;
    }

    public static OperationContext ReportError(
        this OperationContext operationContext,
        IError error,
        MiddlewareContext resolverContext,
        ISelection? selection = null)
    {
        var errors = new List<IError>();

        ReportSingleError(
            operationContext.RequestContext,
            operationContext.ErrorHandler,
            error,
            errors);

        selection ??= resolverContext.Selection;

        foreach (var handled in errors)
        {
            operationContext.Result.AddError(handled, selection);
        }

        operationContext.DiagnosticEvents.ExecutionError(
            operationContext.RequestContext,
            ErrorKind.FieldError,
            errors);

        return operationContext;
    }

    private static void ReportSingleError(
        RequestContext requestContext,
        IErrorHandler errorHandler,
        IError error,
        List<IError> errors,
        int depth = 0)
    {
        if (depth > 4)
        {
            throw new InvalidOperationException(
                "Error reporting depth exceeded. " +
                "Aggregate error are not allowed to be nested beyond 4 levels.");
        }

        var handled = errorHandler.Handle(error);

        if (handled is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                ReportSingleError(
                    requestContext,
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

    public static OperationContext SetLabel(
        this OperationContext context,
        string? label)
    {
        context.Result.SetLabel(label);
        return context;
    }

    public static OperationContext SetPath(
        this OperationContext context,
        Path? path)
    {
        context.Result.SetPath(path);
        return context;
    }

    public static OperationContext SetData(
        this OperationContext context,
        ObjectResult objectResult)
    {
        context.Result.SetData(objectResult);
        return context;
    }

    public static OperationContext SetItems(
        this OperationContext context,
        IReadOnlyList<object?> items)
    {
        context.Result.SetItems(items);
        return context;
    }

    public static OperationContext SetPatchId(
        this OperationContext context,
        uint patchId)
    {
        context.Result.SetContextData(WellKnownContextData.PatchId, patchId);
        return context;
    }

    public static OperationContext ClearResult(
        this OperationContext context)
    {
        context.Result.Clear();
        return context;
    }

    public static IOperationResult BuildResult(
        this OperationContext context) =>
        context.Result.BuildResult();
}
