using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
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
                            throw OperationResolverHelper_MultipleOperation(operation, op);
                        }
                    }
                }

                if (operation is null)
                {
                    throw OperationResolverHelper_NoOperationFound(document);
                }

                return operation;
            }
            else
            {
                for (int i = 0; i < document.Definitions.Count; i++)
                {
                    if (document.Definitions[i] is OperationDefinitionNode { Name: { } } op &&
                        op.Name!.Value.EqualsOrdinal(operationName))
                    {
                        return op;
                    }
                }

                throw OperationResolverHelper_InvalidOperationName(document, operationName);
            }
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
