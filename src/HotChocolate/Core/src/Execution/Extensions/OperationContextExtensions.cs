using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Pooling;

namespace HotChocolate.Execution;

internal static class OperationContextExtensions
{
    public static IOperationContext TrySetNext(
        this IOperationContext context,
        bool alwaysSet = false)
    {
        if (context.Scheduler.DeferredWork.HasWork)
        {
            context.Result.SetHasNext(true);
        }
        else if (alwaysSet)
        {
            context.Result.SetHasNext(false);
        }

        return context;
    }

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
        ObjectResult data)
    {
        context.Result.SetData(data);
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
}
