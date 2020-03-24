using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidator
    {
        DocumentValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }
}
