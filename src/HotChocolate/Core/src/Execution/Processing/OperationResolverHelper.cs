using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal static class OperationResolverHelper
{
    public static OperationDefinitionNode GetOperation(
        this DocumentNode document,
        string? operationName)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            OperationDefinitionNode? operation = null;
            var definitions = document.Definitions;
            var length = definitions.Count;

            for (var i = 0; i < length; i++)
            {
                if (definitions[i] is not OperationDefinitionNode op)
                {
                    continue;
                }

                if (operation is null)
                {
                    operation = op;
                }
                else
                {
                    throw OperationResolverHelper_MultipleOperation(operation, op);
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
            for (var i = 0; i < document.Definitions.Count; i++)
            {
                if (document.Definitions[i] is OperationDefinitionNode { Name: { }, } op &&
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
        var definitions = document.Definitions;
        var length = definitions.Count;
        var map = new Dictionary<string, FragmentDefinitionNode>(StringComparer.Ordinal);

        for (var i = 0; i < length; i++)
        {
            if (definitions[i] is FragmentDefinitionNode fragmentDef)
            {
                map.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return map;
    }
}
