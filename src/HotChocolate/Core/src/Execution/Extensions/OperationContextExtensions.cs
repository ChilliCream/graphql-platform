using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution
{
    internal static class OperationContextExtensions
    {
        public static IOperationContext TrySetNext(
            this IOperationContext context,
            bool alwaysSet = false)
        {
            if (context.Execution.DeferredWork.HasWork)
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
            ResultMap resultMap)
        {
            context.Result.SetData(resultMap);
            return context;
        }

        public static IQueryResult BuildResult(
            this IOperationContext context) =>
            context.Result.BuildResult();
    }
}
