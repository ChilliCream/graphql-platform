using System;

namespace HotChocolate.Execution
{
    internal static class CompleteValueContextExtensions
    {
        public static void CompleteValue(
            this CompleteValueContext2 completionContext,
            ____ResolverContext resolverContext)
        {
            if (completionContext == null)
            {
                throw new ArgumentNullException(nameof(completionContext));
            }

            completionContext.ResolverContext = resolverContext;

            ValueCompletion2.CompleteValue(
                completionContext,
                resolverContext.Field.Type,
                resolverContext.Result);

            if (completionContext.IsViolatingNonNullType)
            {
                resolverContext.PropagateNonNullViolation.Invoke();
            }
            else
            {
                resolverContext.SetCompletedValue(completionContext.Value);
            }
        }
    }
}
