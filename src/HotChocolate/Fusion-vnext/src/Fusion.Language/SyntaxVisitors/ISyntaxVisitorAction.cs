namespace HotChocolate.Fusion;

internal interface ISyntaxVisitorAction
{
    SyntaxVisitorActionKind Kind { get; }
}
