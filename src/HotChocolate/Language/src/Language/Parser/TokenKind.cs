namespace HotChocolate.Language
{
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
        /// The bang token is used to specify
        /// non null types and is represented by:
        /// '!'.
        /// </summary>
        Bang,

        /// <summary>
        /// The dollar token is used to specify variables
        /// and variable declarations and is represented by:
        /// '$'.
        /// </summary>
        Dollar,
        Ampersand,
        LeftParenthesis,
        RightParenthesis,
        Spread,
        Colon,
        Equal,
        At,
        LeftBracket,
        RightBracket,
        LeftBrace,
        RightBrace,
        Pipe,
        Name,
        Integer,
        Float,
        String,
        BlockString,
        Comment
    }
}
