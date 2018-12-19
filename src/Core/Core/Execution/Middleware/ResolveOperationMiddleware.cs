using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class ResolveOperationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly Cache<OperationDefinitionNode> _queryCache;

        public ResolveOperationMiddleware(
            QueryDelegate next,
            Cache<OperationDefinitionNode> queryCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryCache = queryCache
                ?? new Cache<OperationDefinitionNode>(Defaults.CacheSize);
        }

        public Task InvokeAsync(IQueryContext context)
        {
            string operationName = context.Request.OperationName;
            string cacheKey = CreateKey(operationName, context.Request.Query);

            context.Operation = _queryCache.GetOrCreate(cacheKey,
                () => GetOperation(context.Document, operationName));

            return _next(context);
        }

        private string CreateKey(string operationName, string queryText)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                return queryText;
            }
            return $"{operationName}-->{queryText}";
        }

        private static OperationDefinitionNode GetOperation(
            DocumentNode queryDocument, string operationName)
        {
            var operations = queryDocument.Definitions
                .OfType<OperationDefinitionNode>()
                .ToList();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Count == 1)
                {
                    return operations[0];
                }

                // TODO : Resources
                throw new QueryException(
                    "Only queries that contain one operation can be executed " +
                    "without specifying the opartion name.");
            }
            else
            {
                OperationDefinitionNode operation = operations.SingleOrDefault(
                    t => t.Name.Value.EqualsOrdinal(operationName));
                if (operation == null)
                {
                    // TODO : Resources
                    throw new QueryException(
                        $"The specified operation `{operationName}` " +
                        "does not exist.");
                }
                return operation;
            }
        }
    }
}
