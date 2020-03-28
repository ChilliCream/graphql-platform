using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IDocumentValidatorRule
    {
        void Validate(IDocumentValidatorContext context, DocumentNode document);
    }
}
