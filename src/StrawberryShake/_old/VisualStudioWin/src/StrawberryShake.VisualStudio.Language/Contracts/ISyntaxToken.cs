namespace StrawberryShake.VisualStudio.Language
{
    public interface ISyntaxToken
    {
        TokenKind Kind { get; }
        int Start { get; }
        int End { get; }
        int Length { get; }
        int Line { get; }
        int Column { get; }
        string? Value { get; }
        SyntaxToken? Previous { get; }
        SyntaxToken? Next { get; }
    }
}
