using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal static class OperationResolverHelper
    {
        public static OperationDefinitionNode GetOperation(
            this DocumentNode document,
            string? operationName)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                OperationDefinitionNode? operation = null;

                for (int i = 0; i < document.Definitions.Count; i++)
                {
                    if (document.Definitions[i] is OperationDefinitionNode op)
                    {
                        if (operation is null)
                        {
                            operation = op;
                        }
                        else
                        {
                            // todo : throwhelper
                            throw new GraphQLException(
                                "CoreResources.GetOperation_MultipleOperations");
                        }
                    }
                }

                if (operation is null)
                {
                    // todo : throwhelper
                    throw new GraphQLException("no ops");
                }

                return operation;
            }
            else
            {
                for (int i = 0; i < document.Definitions.Count; i++)
                {
                    if (document.Definitions[i] is OperationDefinitionNode { Name: { } } op &&
                        op.Name!.Value.EqualsOrdinal(operationName))
                    {
                        return op;
                    }
                }

                // todo : throwhelper
                throw new GraphQLException(string.Format(
                    CultureInfo.CurrentCulture,
                    "CoreResources.GetOperation_InvalidOperationName",
                    operationName));
            }
        }
    }
}
