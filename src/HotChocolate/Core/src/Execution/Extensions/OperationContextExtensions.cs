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

        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is GraphQLException ex)
        {
            foreach (var error in ex.Errors)
            {
                ReportError(operationContext, error, resolverContext, selection);
            }
        }
        else
        {
            var error = operationContext.ErrorHandler
                .CreateUnexpectedError(exception)
                .SetPath(path)
                .SetLocations([selection.SyntaxNode])
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
        selection ??= resolverContext.Selection;

        if (error is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                ReportSingleError(operationContext, innerError, resolverContext, selection);
            }
        }
        else
        {
            ReportSingleError(operationContext, error, resolverContext, selection);
        }

        return operationContext;
    }

    private static void ReportSingleError(
        OperationContext operationContext,
        IError error,
        MiddlewareContext resolverContext,
        ISelection selection)
    {
        var handled = operationContext.ErrorHandler.Handle(error);

        if (handled is AggregateError ar)
        {
            foreach (var ie in ar.Errors)
            {
                operationContext.Result.AddError(ie, selection);
                operationContext.DiagnosticEvents.ResolverError(resolverContext, ie);
            }
        }
        else
        {
            operationContext.Result.AddError(handled, selection);
            operationContext.DiagnosticEvents.ResolverError(resolverContext, handled);
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
