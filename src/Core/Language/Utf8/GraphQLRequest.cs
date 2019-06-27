using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public readonly struct GraphQLRequest
    {
        public GraphQLRequest(
            string operationName,
            string namedQuery,
            DocumentNode query,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> extensions)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            OperationName = operationName;
            NamedQuery = namedQuery;
            Query = query;
            Variables = variables;
            Extensions = extensions;
        }

        public string OperationName { get; }

        public string NamedQuery { get; }

        public DocumentNode Query { get; }

        public IReadOnlyDictionary<string, object> Variables { get; }

        public IReadOnlyDictionary<string, object> Extensions { get; }
    }
}
