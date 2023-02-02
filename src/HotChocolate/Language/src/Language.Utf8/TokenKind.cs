namespace HotChocolate.Language;

/// <summary>
/// Represents the token kinds.
/// </summary>
public enum TokenKind : byte
{
    /// <summary>
    /// The start of file token.
    /// </summary>
    StartOfFile,

    /// <summary>
    /// The end of file token.
    /// </summary>
    EndOfFile,

    /// <summary>
    /// !
    /// </summary>
    Bang,

    /// <summary>
    /// ?
    /// </summary>
    QuestionMark,

    /// <summary>
    /// $
    /// </summary>
    Dollar,

    /// <summary>
    /// &amp;
    /// </summary>
    Ampersand,

    /// <summary>
    /// (
    /// </summary>
    LeftParenthesis,

    /// <summary>
    /// )
    /// </summary>
    RightParenthesis,

    /// <summary>
    /// ...
    /// </summary>
    Spread,

    /// <summary>
    /// :
    /// </summary>
    Colon,

    /// <summary>
    /// =
    /// </summary>
    Equal,

    /// <summary>
    /// @
    /// </summary>
    At,

    /// <summary>
    /// [
    /// </summary>
    LeftBracket,

    /// <summary>
    /// ]
    /// </summary>
    RightBracket,

    /// <summary>
    /// {
    /// </summary>
    LeftBrace,

    /// <summary>
    /// }
    /// </summary>
    RightBrace,

    /// <summary>
    /// |
    /// </summary>
    Pipe,

    /// <summary>
    /// A name token.
    /// </summary>
    Name,

    /// <summary>
    /// A integer token.
    /// </summary>
    Integer,

    /// <summary>
    /// A float token.
    /// </summary>
    Float,

    /// <summary>
    /// A string token.
    /// </summary>
    String,

    /// <summary>
    /// A block string token.
    /// </summary>
    BlockString,

    /// <summary>
    /// A comment token.
    /// </summary>
    Comment,

    /// <summary>
    /// .
    /// </summary>
    Dot,
}
