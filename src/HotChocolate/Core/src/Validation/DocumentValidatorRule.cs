using HotChocolate.Language;

namespace HotChocolate.Validation;

public class DocumentValidatorRule<TVisitor>
    : IDocumentValidatorRule
    where TVisitor : DocumentValidatorVisitor
{
    private readonly TVisitor _visitor;

    public DocumentValidatorRule(TVisitor visitor, bool isCacheable = true)
    {
        _visitor = visitor;
        IsCacheable = isCacheable;
    }

    public bool IsCacheable { get; }

    public void Validate(IDocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        _visitor.Visit(document, context);
    }
}
