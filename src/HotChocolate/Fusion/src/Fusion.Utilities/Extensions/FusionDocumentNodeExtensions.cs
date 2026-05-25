using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public static class FusionDocumentNodeExtensions
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
                if (document.Definitions[i] is OperationDefinitionNode { Name: { } } op
                    && op.Name!.Value.Equals(operationName, StringComparison.Ordinal))
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

    private static GraphQLException OperationResolverHelper_NoOperationFound(
        DocumentNode documentNode) =>
        new(ErrorBuilder.New()
            .SetMessage("There are no operations in the GraphQL document.")
            .AddLocation(documentNode)
            .Build());

    private static GraphQLException OperationResolverHelper_MultipleOperation(
        OperationDefinitionNode firstOperation,
        OperationDefinitionNode secondOperation) =>
        new(ErrorBuilder.New()
            .SetMessage("The operation name can only be omitted if there is just one operation in a GraphQL document.")
            .AddLocation(firstOperation)
            .AddLocation(secondOperation)
            .Build());

    private static GraphQLException OperationResolverHelper_InvalidOperationName(
        DocumentNode documentNode,
        string operationName) =>
        new(ErrorBuilder.New()
            .SetMessage("The specified operation `{0}` cannot be found.", operationName)
            .AddLocation(documentNode)
            .SetExtension("operationName", operationName)
            .Build());
}
