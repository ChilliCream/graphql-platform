using HotChocolate.Language;

namespace HotChocolate.Validation;

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

    public override string? ToString()
        => _visitor.GetType().FullName;
}
