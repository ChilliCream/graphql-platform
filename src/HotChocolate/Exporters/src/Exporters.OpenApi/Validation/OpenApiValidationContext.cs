namespace HotChocolate.Exporters.OpenApi.Validation;

internal sealed class OpenApiValidationContext : IOpenApiValidationContext
{
    private readonly List<OpenApiFragmentDocument> _validFragments = [];
    private readonly List<OpenApiOperationDocument> _validOperations = [];

    public void AddValidDocument(IOpenApiDocument document)
    {
        if (document is OpenApiOperationDocument operationDocument)
        {
            _validOperations.Add(operationDocument);
        }
        else if (document is OpenApiFragmentDocument fragmentDocument)
        {
            _validFragments.Add(fragmentDocument);
        }
    }

    public List<OpenApiFragmentDocument> ValidFragmentDocuments => _validFragments;

    public List<OpenApiOperationDocument> ValidOperationDocuments => _validOperations;
}
