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
                    tokens.Add(input.CreateToken(FilterTokenKind.LeftParenthesis, position, position + 1));
                    position++;
                    continue;
                case ')':
                    tokens.Add(input.CreateToken(FilterTokenKind.RightParenthesis, position, position + 1));
                    position++;
                    continue;
                case ',':
                    tokens.Add(input.CreateToken(FilterTokenKind.Comma, position, position + 1));
                    position++;
                    continue;
                case ':':
                    tokens.Add(input.CreateToken(FilterTokenKind.Colon, position, position + 1));
                    position++;
                    continue;
                case '-':
                    tokens.Add(input.CreateToken(FilterTokenKind.Minus, position, position + 1));
                    position++;
                    continue;
                case '*':
                    if (!input.IsWordContinue(position + 1))
                    {
                        tokens.Add(input.CreateToken(FilterTokenKind.Star, position, position + 1));
                        position++;
                        continue;
                    }

                    break;
                case '>':
                    if (position + 1 < input.Length && input[position + 1] == '=')
                    {
                        tokens.Add(input.CreateToken(FilterTokenKind.GreaterThanOrEqual, position, position + 2));
                        position += 2;
                    }
                    else
                    {
                        tokens.Add(input.CreateToken(FilterTokenKind.GreaterThan, position, position + 1));
                        position++;
                    }

                    continue;
                case '<':
                    if (position + 1 < input.Length && input[position + 1] == '=')
                    {
                        tokens.Add(input.CreateToken(FilterTokenKind.LessThanOrEqual, position, position + 2));
                        position += 2;
                    }
                    else
                    {
                        tokens.Add(input.CreateToken(FilterTokenKind.LessThan, position, position + 1));
                        position++;
                    }

                    continue;
                case '"':
                    tokens.Add(input.ReadString(ref position));
                    continue;
            }

            if (character.IsWordStart() || character == '*')
            {
                tokens.Add(input.ReadWord(ref position));
                continue;
            }

            throw FilterParseException.FromPosition($"Unexpected character '{character}'", position);
        }

        tokens.Add(new FilterToken(FilterTokenKind.End, string.Empty, string.Empty, position, position));
        return tokens;
    }
}

file static class Extensions
{
    extension(FilterParseException)
    {
        public static FilterParseException FromPosition(string message, int position)
            => new(message, position + 1);
    }

    extension(char character)
    {
        public bool IsWordStart()
            => character.IsWordContinue() && character is not '-' and not '*';

        public bool IsWordContinue()
            => !character.IsWhitespace() && character is not '(' and not ')' and not ',' and not ':' and not '"' and not '<' and not '>' and not '!';

        public bool IsWhitespace()
            => character is ' ' or '\t' or '\n' or '\r';
    }

    extension(ref int length)
    {
        public void CheckWildcardRun(ref int repeatedStart, int runStart)
        {
            if (length > 1 && repeatedStart < 0)
            {
                repeatedStart = runStart;
            }

            length = 0;
        }
    }

    extension(string input)
    {
        public bool IsWordContinue(int position)
            => position < input.Length && input[position].IsWordContinue();

        public FilterToken CreateToken(FilterTokenKind kind, int start, int end)
            => new(kind, input[start..end], input[start..end], start, end);

        public FilterToken ReadString(ref int position)
        {
            var start = position;
            position++;
            var value = new System.Text.StringBuilder();

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

            throw FilterParseException.FromPosition("Missing closing quote", start);
        }

        public FilterToken ReadWord(ref int position)
        {
            var start = position;
            var value = new System.Text.StringBuilder();
            var hadEscape = false;
            var hasWildcard = false;
            var wildcardRunStart = -1;
            var wildcardRunLength = 0;
            var repeatedWildcardStart = -1;

            while (position < input.Length && input.IsWordContinue(position))
            {
                var character = input[position];
                if (character == '\\')
                {
                    wildcardRunLength.CheckWildcardRun(ref repeatedWildcardStart, wildcardRunStart);
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
                    wildcardRunLength.CheckWildcardRun(ref repeatedWildcardStart, wildcardRunStart);
                }

                value.Append(character);
                position++;
            }

            wildcardRunLength.CheckWildcardRun(ref repeatedWildcardStart, wildcardRunStart);
            if (repeatedWildcardStart >= 0)
            {
                var isExists =
                    tokensAfterColon(input, start)
                    && repeatedWildcardStart == start
                    && input[start..position].All(static character => character == '*');
                throw FilterParseException.FromPosition(
                    isExists
                        ? "A single * after the colon already checks the attribute exists"
                        : "A single * already matches any text",
                    repeatedWildcardStart + 1);
            }

            var text = input[start..position];
            return new FilterToken(
                hadEscape ? FilterTokenKind.Word : text.ClassifyWord(input, position),
                text,
                value.ToString(),
                start,
                position,
                hasWildcard);

            static bool tokensAfterColon(string source, int tokenStart)
                => tokenStart > 0 && source[tokenStart - 1] == ':';
        }

        public FilterTokenKind ClassifyWord(string source, int position)
        {
            var nextCharacter = position < source.Length ? source[position] : '\0';
            return input switch
            {
                "AND" => FilterTokenKind.And,
                "OR" => FilterTokenKind.Or,
                "IN" when nextCharacter == '(' => FilterTokenKind.In,
                "RANGE" when nextCharacter == '(' => FilterTokenKind.Range,
                "true" or "false" => FilterTokenKind.Boolean,
                _ when input.IsNumber() => FilterTokenKind.Number,
                _ => FilterTokenKind.Word
            };
        }

        public bool IsNumber()
        {
            var separator = input.IndexOf('.');
            if (separator < 0)
            {
                return input.All(char.IsAsciiDigit);
            }

            if (separator == 0 || separator == input.Length - 1 || separator != input.LastIndexOf('.'))
            {
                return false;
            }

            return input[..separator].All(char.IsAsciiDigit) && input[(separator + 1)..].All(char.IsAsciiDigit);
        }
    }
}
