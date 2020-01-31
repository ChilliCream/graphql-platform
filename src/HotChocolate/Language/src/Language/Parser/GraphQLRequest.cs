using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class GraphQLRequest
    {
        public GraphQLRequest(DocumentNode query)
            : this(query, null, null, null, null, null)
        {
        }

        public GraphQLRequest(DocumentNode query, string queryName)
            : this(query, queryName, null, null, null, null)
        {
        }

        public GraphQLRequest(
            DocumentNode? query,
            string? queryName,
            string? queryHash,
            string? operationName,
            IReadOnlyDictionary<string, object?>? variables,
            IReadOnlyDictionary<string, object?>? extensions)
        {
            if (query is null && queryName is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            OperationName = operationName;
            QueryName = queryName;
            QueryHash = queryHash;
            Query = query;
            Variables = variables;
            Extensions = extensions;
        }

        public DocumentNode? Query { get; }

        public string? QueryName { get; }

        public string? QueryHash { get; }

        public string? OperationName { get; }

        public IReadOnlyDictionary<string, object?>? Variables { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }
    }
}
