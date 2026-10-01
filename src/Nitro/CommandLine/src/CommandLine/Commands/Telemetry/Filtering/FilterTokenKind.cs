namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal enum FilterTokenKind
{
    Word,
    String,
    Number,
    Boolean,
    Colon,
    LeftParenthesis,
    RightParenthesis,
    Comma,
    Minus,
    Star,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    And,
    Or,
    In,
    Range,
    End
}
