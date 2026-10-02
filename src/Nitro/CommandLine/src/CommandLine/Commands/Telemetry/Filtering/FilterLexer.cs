using System.Text;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterLexer
{
    public static IReadOnlyList<FilterToken> Tokenize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var tokens = new List<FilterToken>();
        var position = 0;

        while (position < input.Length)
        {
            var character = input[position];

            if (character.IsWhitespace())
            {
                position++;
                continue;
            }

            switch (character)
            {
                case '(':
                    tokens.Add(CreateToken(FilterTokenKind.LeftParenthesis, input, position, position + 1));
                    position++;
                    continue;
                case ')':
                    tokens.Add(CreateToken(FilterTokenKind.RightParenthesis, input, position, position + 1));
                    position++;
                    continue;
                case ',':
                    tokens.Add(CreateToken(FilterTokenKind.Comma, input, position, position + 1));
                    position++;
                    continue;
                case ':':
                    tokens.Add(CreateToken(FilterTokenKind.Colon, input, position, position + 1));
                    position++;
                    continue;
                case '-':
                    tokens.Add(CreateToken(FilterTokenKind.Minus, input, position, position + 1));
                    position++;
                    continue;
                case '*':
                    if (!IsWordContinue(input, position + 1))
                    {
                        tokens.Add(CreateToken(FilterTokenKind.Star, input, position, position + 1));
                        position++;
                        continue;
                    }

                    break;
                case '>':
                    if (position + 1 < input.Length && input[position + 1] == '=')
                    {
                        tokens.Add(CreateToken(FilterTokenKind.GreaterThanOrEqual, input, position, position + 2));
                        position += 2;
                    }
                    else
                    {
                        tokens.Add(CreateToken(FilterTokenKind.GreaterThan, input, position, position + 1));
                        position++;
                    }

                    continue;
                case '<':
                    if (position + 1 < input.Length && input[position + 1] == '=')
                    {
                        tokens.Add(CreateToken(FilterTokenKind.LessThanOrEqual, input, position, position + 2));
                        position += 2;
                    }
                    else
                    {
                        tokens.Add(CreateToken(FilterTokenKind.LessThan, input, position, position + 1));
                        position++;
                    }

                    continue;
                case '"':
                    tokens.Add(ReadString(input, ref position));
                    continue;
            }

            if (character.IsWordStart() || character == '*')
            {
                tokens.Add(ReadWord(input, ref position));
                continue;
            }

            throw ThrowHelper.InvalidFilterSyntax($"Unexpected character '{character}'", position);
        }

        tokens.Add(new FilterToken(FilterTokenKind.End, string.Empty, string.Empty, position, position));
        return tokens;
    }

    private static FilterToken ReadString(string input, ref int position)
    {
        var start = position;
        position++;
        var value = new StringBuilder();

        while (position < input.Length)
        {
            var character = input[position];
            if (character == '\\' && position + 1 < input.Length)
            {
                value.Append(input[position + 1]);
                position += 2;
                continue;
            }

            if (character == '"')
            {
                position++;
                return new FilterToken(
                    FilterTokenKind.String,
                    input[start..position],
                    value.ToString(),
                    start,
                    position);
            }

            value.Append(character);
            position++;
        }

        throw ThrowHelper.InvalidFilterSyntax("Missing closing quote", start);
    }

    private static FilterToken ReadWord(string input, ref int position)
    {
        var start = position;
        var value = new StringBuilder();
        var hadEscape = false;
        var hasWildcard = false;
        var wildcardRunStart = -1;
        var wildcardRunLength = 0;
        var repeatedWildcardStart = -1;

        while (position < input.Length && IsWordContinue(input, position))
        {
            var character = input[position];
            if (character == '\\')
            {
                EndWildcardRun(ref wildcardRunLength, ref repeatedWildcardStart, wildcardRunStart);
                hadEscape = true;
                if (position + 1 < input.Length)
                {
                    value.Append(input[position + 1]);
                    position += 2;
                }
                else
                {
                    value.Append('\\');
                    position++;
                }

                continue;
            }

            if (character == '*')
            {
                hasWildcard = true;
                if (wildcardRunLength == 0)
                {
                    wildcardRunStart = position;
                }

                wildcardRunLength++;
            }
            else
            {
                EndWildcardRun(ref wildcardRunLength, ref repeatedWildcardStart, wildcardRunStart);
            }

            value.Append(character);
            position++;
        }

        EndWildcardRun(ref wildcardRunLength, ref repeatedWildcardStart, wildcardRunStart);
        if (repeatedWildcardStart >= 0)
        {
            var isExists =
                tokensAfterColon(input, start)
                && repeatedWildcardStart == start
                && input[start..position].All(static character => character == '*');
            throw ThrowHelper.InvalidFilterSyntax(
                isExists
                    ? "A single * after the colon already checks the attribute exists"
                    : "A single * already matches any text",
                repeatedWildcardStart + 1);
        }

        var text = input[start..position];
        return new FilterToken(
            hadEscape ? FilterTokenKind.Word : ClassifyWord(text, input, position),
            text,
            value.ToString(),
            start,
            position,
            hasWildcard);

        static bool tokensAfterColon(string source, int tokenStart) => tokenStart > 0 && source[tokenStart - 1] == ':';
    }

    private static void EndWildcardRun(ref int length, ref int repeatedStart, int runStart)
    {
        if (length > 1 && repeatedStart < 0)
        {
            repeatedStart = runStart;
        }

        length = 0;
    }

    private static FilterToken CreateToken(FilterTokenKind kind, string input, int start, int end)
        => new(kind, input[start..end], input[start..end], start, end);

    private static FilterTokenKind ClassifyWord(string text, string input, int position)
    {
        var nextCharacter = position < input.Length ? input[position] : '\0';
        return text switch
        {
            "AND" => FilterTokenKind.And,
            "OR" => FilterTokenKind.Or,
            "IN" when nextCharacter == '(' => FilterTokenKind.In,
            "RANGE" when nextCharacter == '(' => FilterTokenKind.Range,
            "true" or "false" => FilterTokenKind.Boolean,
            _ when IsNumber(text) => FilterTokenKind.Number,
            _ => FilterTokenKind.Word
        };
    }

    private static bool IsNumber(string text)
    {
        var separator = text.IndexOf('.');
        if (separator < 0)
        {
            return text.All(char.IsAsciiDigit);
        }

        if (separator == 0
            || separator == text.Length - 1
            || separator != text.LastIndexOf('.'))
        {
            return false;
        }

        return text[..separator].All(char.IsAsciiDigit) && text[(separator + 1)..].All(char.IsAsciiDigit);
    }

    private static bool IsWordContinue(string input, int position)
        => position < input.Length && input[position].IsWordContinue();
}

file static class Extensions
{
    extension(char character)
    {
        public bool IsWordStart() => character.IsWordContinue() && character is not '-' and not '*';

        public bool IsWordContinue()
            => !character.IsWhitespace()
            && character is not '(' and not ')' and not ',' and not ':' and not '"' and not '<' and not '>' and not '!';

        public bool IsWhitespace() => character is ' ' or '\t' or '\n' or '\r';
    }
}
