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
}
