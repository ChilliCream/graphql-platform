using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidationRule
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }
}
