using HotChocolate.Language;

namespace HotChocolate.Validation;

public class DocumentValidatorRule<TVisitor>
    : IDocumentValidatorRule
    where TVisitor : DocumentValidatorVisitor
{
    private readonly TVisitor _visitor;

    public DocumentValidatorRule(
        TVisitor visitor,
        bool isCacheable = true,
        ushort property = ushort.MaxValue)
    {
        _visitor = visitor;
        IsCacheable = isCacheable;
        Priority = property;
    }

    public ushort Priority { get; }

    public bool IsCacheable { get; }

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _visitor.Visit(document, context);
    }
}

public sealed class DocumentValidatorRule : IDocumentValidatorRule
{
    private readonly DocumentValidatorVisitor _visitor;

    public DocumentValidatorRule(
        DocumentValidatorVisitor visitor,
        bool isCacheable = true,
        ushort priority = ushort.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        _visitor = visitor;
        IsCacheable = isCacheable;
        Priority = priority;
    }

    public ushort Priority { get; }

    public bool IsCacheable { get; }

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        _visitor.Visit(document, context);
    }
}
