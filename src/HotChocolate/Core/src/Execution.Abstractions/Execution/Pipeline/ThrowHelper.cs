using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class ThrowHelper
{
    public static InvalidOperationException NormalizedDocument_NoDocument()
        => new("The request context has no operation document to normalize.");

    public static InvalidOperationException NormalizedDocument_DocumentNotValidated()
        => new("The operation document must be validated before it can be normalized.");

    public static InvalidOperationException NormalizedDocument_DocumentIdEmpty()
        => new("The operation document must have a document ID before it can be normalized.");

    public static InvalidOperationException OperationId_NoDocument()
        => new("The request context has no operation document to compute an operation id from.");

    public static InvalidOperationException OperationId_DocumentIdEmpty()
        => new("The operation document must have a document ID before an operation id can be computed.");

    public static GraphQLException OperationId_OperationNotFound(
        DocumentNode document,
        string operationName)
        => new(ErrorBuilder.New()
            .SetMessage("The specified operation `{0}` cannot be found.", operationName)
            .AddLocation(document)
            .SetExtension("operationName", operationName)
            .Build());

    public static GraphQLException OperationId_OperationNameRequired(DocumentNode document)
    {
        var builder = ErrorBuilder.New()
            .SetMessage("The operation name can only be omitted if there is just one operation in a GraphQL document.");
        var count = 0;

        for (var i = 0; i < document.Definitions.Count && count < 2; i++)
        {
            if (document.Definitions[i] is OperationDefinitionNode operation)
            {
                builder.AddLocation(operation);
                count++;
            }
        }

        return new GraphQLException(builder.Build());
    }

    public static GraphQLException OperationId_NoOperationFound(DocumentNode document)
        => new(ErrorBuilder.New()
            .SetMessage("There are no operations in the GraphQL document.")
            .AddLocation(document)
            .Build());
}
