using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public class DocumentValidatorRule<TVisitor>
        : IDocumentValidatorRule
        where TVisitor : DocumentValidatorVisitor, new()
    {
        private readonly TVisitor _visitor = new TVisitor();

        public void Validate(IDocumentValidatorContext context, DocumentNode document)
        {
            _visitor.Visit(document, context);
        }
    }
}
