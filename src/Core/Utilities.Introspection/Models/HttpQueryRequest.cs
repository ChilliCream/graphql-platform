namespace HotChocolate.Utilities.Introspection
{
    internal class HttpQueryRequest
    {
        public HttpQueryRequest(string query, string operationName)
        {
            Query = query;
            OperationName = operationName;
        }

        public string Query { get; set; }

        public string OperationName { get; set; }
    }
}
