namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents the token kinds in <c>FieldSelectionMap</c> source text.
/// </summary>
internal enum FieldSelectionMapTokenKind
{
    /// <summary>:</summary>
    Colon,

    /// <summary>The end of file token.</summary>
    EndOfFile,

    /// <summary>&lt;</summary>
    LeftAngleBracket,

    /// <summary>{</summary>
    LeftBrace,

    /// <summary>[</summary>
    LeftSquareBracket,

    /// <summary>A name token.</summary>
    Name,

    /// <summary>.</summary>
    Period,

    /// <summary>|</summary>
    Pipe,

    /// <summary>&gt;</summary>
    RightAngleBracket,

    /// <summary>]</summary>
    RightSquareBracket,

    /// <summary>}</summary>
    RightBrace,

    /// <summary>The start of file token.</summary>
    StartOfFile
}
