using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public partial class QueryExecuter
    {
        private OperationExecuter GetOrCreateOperationExecuter(
            QueryRequest queryRequest, DocumentNode queryDocument)
        {
            if (_useCache)
            {
                string operationKey = CreateOperationKey(
                    queryRequest.Query, queryRequest.OperationName);
                return _operationCache.GetOrCreate(operationKey,
                    () => CreateOperationExecuter(queryRequest, queryDocument));
            }
            return CreateOperationExecuter(queryRequest, queryDocument);
        }

        private string CreateOperationKey(string query, string operationName)
        {
            string normalizedQuery = NormalizeQuery(query);

            if (operationName == null)
            {
                return normalizedQuery;
            }

            return $"{operationName}++{query}";
        }

        private OperationExecuter CreateOperationExecuter(
            QueryRequest queryRequest, DocumentNode queryDocument)
        {
            return new OperationExecuter(
                Schema, queryDocument, GetOperation(
                    queryDocument, queryRequest.OperationName));
        }

        private static OperationDefinitionNode GetOperation(
            DocumentNode queryDocument, string operationName)
        {
            OperationDefinitionNode[] operations = queryDocument.Definitions
                .OfType<OperationDefinitionNode>()
                .ToArray();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Length == 1)
                {
                    return operations[0];
                }

                throw new QueryException(
                    "Only queries that contain one operation can be executed " +
                    "without specifying the opartion name.");
            }
            else
            {
                OperationDefinitionNode operation = operations.SingleOrDefault(
                    t => string.Equals(t.Name.Value, operationName, StringComparison.Ordinal));
                if (operation == null)
                {
                    throw new QueryException(
                        $"The specified operation `{operationName}` does not exist.");
                }
                return operation;
            }
        }
    }
}
