namespace HotChocolate.Fusion.Language;

public interface ISyntaxVisitorAction
{
    SyntaxVisitorActionKind Kind { get; }
}
