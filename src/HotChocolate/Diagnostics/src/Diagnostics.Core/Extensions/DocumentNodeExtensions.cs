using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal static class DocumentNodeExtensions
{
    public static OperationDefinitionNode? GetOperation(
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
                    // More than one operation in document.
                    return null;
                }
            }

            return operation;
        }
        else
        {
            for (var i = 0; i < document.Definitions.Count; i++)
            {
                if (document.Definitions[i] is OperationDefinitionNode { Name: not null } op
                    && op.Name!.Value.Equals(operationName, StringComparison.Ordinal))
                {
                    return op;
                }
            }

            return null;
        }
    }
}
