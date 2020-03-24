using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public class DocumentValidationRule<TVisitor>
        : IDocumentValidationRule
        where TVisitor : DocumentValidationVisitor, new()
    {
        private readonly DocumentValidationContextPool _contextPool;
        private readonly TVisitor _visitor = new TVisitor();

        protected DocumentValidationRule(DocumentValidationContextPool contextPool)
        {
            _contextPool = contextPool;
        }

        public DocumentValidationResult Validate(ISchema schema, DocumentNode document)
        {
            DocumentValidationContext context = _contextPool.Get();
            try
            {
                _visitor.Visit(document, context);

                return context.Errors.Count == 0
                    ? DocumentValidationResult.OK
                    : new DocumentValidationResult(context.Errors);
            }
            finally
            {
                _contextPool.Return(context);
            }
        }
    }
}
