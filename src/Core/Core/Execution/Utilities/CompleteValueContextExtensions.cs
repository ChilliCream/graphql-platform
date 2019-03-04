using System;

namespace HotChocolate.Execution
{
    internal static class CompleteValueContextExtensions
    {
        public static void CompleteValue(
            this CompleteValueContext context,
            ResolverTask resolverTask)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ResolverTask = resolverTask;

            ValueCompletion.CompleteValue(
                context,
                resolverTask.FieldType,
                resolverTask.ResolverResult);

            if (context.IsViolatingNonNullType)
            {
                resolverTask.PropagateNonNullViolation();
            }
            else
            {
                resolverTask.SetResult(context.Value);
            }
        }
    }
}
