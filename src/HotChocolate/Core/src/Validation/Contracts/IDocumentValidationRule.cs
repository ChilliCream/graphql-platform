using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidationRule
    {
        DocumentValidationResult Validate(
            ISchema schema,
            DocumentNode document);
    }
}
