using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class GraphQLRequest
    {
        public GraphQLRequest(
            DocumentNode? query,
            string? queryId = null,
            string? queryHash = null,
            string? operationName = null,
            IReadOnlyDictionary<string, object?>? variables = null,
            IReadOnlyDictionary<string, object?>? extensions = null)
        {
            if (query is null && queryId is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            OperationName = operationName;
            QueryId = queryId;
            QueryHash = queryHash;
            Query = query;
            Variables = variables;
            Extensions = extensions;
        }

        public string? QueryId { get; }

        public DocumentNode? Query { get; }

        public string? QueryHash { get; }

        public string? OperationName { get; }

        public IReadOnlyDictionary<string, object?>? Variables { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }
    }
}
