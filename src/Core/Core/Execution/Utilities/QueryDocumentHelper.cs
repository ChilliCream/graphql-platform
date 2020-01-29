using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal static class QueryDocumentHelper
    {
        public static OperationDefinitionNode GetOperation(
            this DocumentNode queryDocument,
            string operationName)
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

                throw new QueryException(
                    CoreResources.GetOperation_MultipleOperations);
            }

            OperationDefinitionNode operation = operations.SingleOrDefault(
                t => t.Name is { } && t.Name.Value.EqualsOrdinal(operationName));

            if (operation == null)
            {
                throw new QueryException(string.Format(
                    CultureInfo.CurrentCulture,
                    CoreResources.GetOperation_InvalidOperationName,
                    operationName));
            }

            return operation;
        }

        public static Dictionary<string, FragmentDefinitionNode> GetFragments(
            this DocumentNode document)
        {
            return document.Definitions
                .OfType<FragmentDefinitionNode>()
                .ToDictionary(t => t.Name.Value);
        }
    }
}
