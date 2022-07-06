using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    public static IOperationContext SetLabel(
        this IOperationContext context,
        string? label)
    {
        context.Result.SetLabel(label);
        return context;
    }

    public static IOperationContext SetPath(
        this IOperationContext context,
        Path? path)
    {
        context.Result.SetPath(path);
        return context;
    }

    public static IOperationContext SetData(
        this IOperationContext context,
        ObjectResult objectResult)
    {
        context.Result.SetData(objectResult);
        return context;
    }

    public static IOperationContext ClearResult(
        this IOperationContext context)
    {
        context.Result.Clear();
        return context;
    }

    public static IQueryResult BuildResult(
        this IOperationContext context) =>
        context.Result.BuildResult();

    public static IQueryResultBuilder BuildResultBuilder(
        this IOperationContext context) =>
        context.Result.BuildResultBuilder();
}
