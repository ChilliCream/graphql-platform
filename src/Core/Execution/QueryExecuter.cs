using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryExecuter
    {
        private VariableValueResolver _variableValueResolver =
            new VariableValueResolver();

        public async Task<QueryResult> ExecuteAsync(
            Schema schema,
            DocumentNode queryDocument,
            string operationName,
            IReadOnlyDictionary<string, IValueNode> variables,
            object initalValue)
        {
            OperationDefinitionNode operation =
                GetOperation(queryDocument, operationName);
            Dictionary<string, object> coercedVariableValues =
                _variableValueResolver.CoerceVariableValues(schema, operation, variables);

            throw new NotImplementedException();
        }

        private Task<QueryResult> ExecuteQueryAsync(
            Schema schema,
            OperationDefinitionNode operation,
            Dictionary<string, IValueNode> variableValues)
        {
            throw new NotImplementedException();
        }

        private Task<QueryResult> ExecuteMutationAsync(
            Schema schema,
            OperationDefinitionNode operation,
            Dictionary<string, IValueNode> variableValues)
        {
            throw new NotImplementedException();
        }

        private Task<QueryResult> SubscribeAsync()
        {
            throw new NotImplementedException();
        }

        private OperationDefinitionNode GetOperation(
            DocumentNode queryDocument,
            string operationName)
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
                throw new Exception();
            }
            throw new NotImplementedException();

        }
    }
}
