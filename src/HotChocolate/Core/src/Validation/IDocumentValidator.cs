using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidator
    {
        DocumentValidatorResult Validate(
            ISchema schema,
            DocumentNode document);
    }
}
