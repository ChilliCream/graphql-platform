using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal static class FusionDocumentNodeExtensions
{
    private const string NoOperationFoundMessage =
        "There are no operations in the GraphQL document.";

    private const string MultipleOperationMessage =
        "The operation name can only be omitted if there is just one operation in a GraphQL document.";

    private const string InvalidOperationNameMessage =
        "The specified operation `{0}` cannot be found.";

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
            .SetMessage(NoOperationFoundMessage)
            .AddLocation(documentNode)
            .Build());

    private static GraphQLException OperationResolverHelper_MultipleOperation(
        OperationDefinitionNode firstOperation,
        OperationDefinitionNode secondOperation) =>
        new(ErrorBuilder.New()
            .SetMessage(MultipleOperationMessage)
            .AddLocation(firstOperation)
            .AddLocation(secondOperation)
            .Build());

    private static GraphQLException OperationResolverHelper_InvalidOperationName(
        DocumentNode documentNode,
        string operationName) =>
        new(ErrorBuilder.New()
            .SetMessage(InvalidOperationNameMessage, operationName)
            .AddLocation(documentNode)
            .SetExtension("operationName", operationName)
            .Build());
}
