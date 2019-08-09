namespace HotChocolate.Execution
{
    internal static class MiddlewareErrorCodes
    {
        public const string Incomplete = "EXEC_MIDDLEWARE_INCOMPLETE";
        public const string Timeout = "EXEC_TIMEOUT";
        public const string QueryNotFound = "PERSISTED_QUERY_NOT_FOUND";
    }
}
