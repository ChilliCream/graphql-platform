using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidator
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }
}
