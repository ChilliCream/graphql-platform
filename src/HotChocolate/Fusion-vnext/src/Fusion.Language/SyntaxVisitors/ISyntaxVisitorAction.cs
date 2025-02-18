namespace HotChocolate.Fusion.Language;

internal interface ISyntaxVisitorAction
{
    SyntaxVisitorActionKind Kind { get; }
}
