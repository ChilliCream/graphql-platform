namespace HotChocolate.Execution
{
    internal static class QueryContextExtensions
    {
        private const string _apolloTracing = "ApolloTracing";
        private const string _apolloTracingStartTimeOffset = _apolloTracing +
            ".StartTimestamp";

        public static string GetApolloTracingActivityName(
            this IQueryContext context)
        {
            return _apolloTracing;
        }

        public static long GetApolloTracingStartTimestamp(
            this IQueryContext context)
        {
            if (context.ContextData.TryGetValue(
                _apolloTracingStartTimeOffset,
                out object v) && v is long value)
            {
                return value;
            }

            return default;
        }

        public static void SetApolloTracingStartTimestamp(
            this IQueryContext context,
            long startTimeOffset)
        {
            context.ContextData.Add(
                _apolloTracingStartTimeOffset,
                startTimeOffset);
        }
    }
}
