using System;

namespace HotChocolate.Language
{
    public enum TokenKind
    {
        StartOfFile,
        EndOfFile,
        Bang,
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