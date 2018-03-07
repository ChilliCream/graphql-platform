using Zeus.Abstractions;

namespace Zeus.Execution
{
    public interface IDocumentValidator
    {
        DocumentValidationReport Validate(ISchema schema, QueryDocument document);
    }
}