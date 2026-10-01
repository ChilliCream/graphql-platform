using System.Collections.Immutable;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterParser
{
    private static readonly ImmutableArray<string> s_traceScopes = ["@span", "@event", "@resource"];
    private static readonly ImmutableArray<string> s_logScopes = ["@log", "@body", "@resource"];

    public static FilterNode? Parse(string input, TelemetryFilterSignal signal)
    {
        var scopes = signal == TelemetryFilterSignal.Traces ? s_traceScopes : s_logScopes;
        return new Parser(FilterLexer.Tokenize(input), scopes).ParseRoot();
    }

    private sealed class Parser(IReadOnlyList<FilterToken> tokens, ImmutableArray<string> scopes)
    {
        private int _index;

        public FilterNode? ParseRoot()
        {
            if (Peek().Kind == FilterTokenKind.End)
            {
                return null;
            }

            var node = ParseOr();
            if (Peek().Kind != FilterTokenKind.End)
            {
                var token = Peek();
                throw FilterParseException.FromPosition($"Expected whitespace but \"{token.Text}\" found", token.Start);
            }

            return node;
        }

        private FilterNode ParseOr()
        {
            var first = ParseAnd();
            if (Peek().Kind != FilterTokenKind.Or)
            {
                return first;
            }

            var children = new List<FilterNode> { first };
            while (Peek().Kind == FilterTokenKind.Or)
            {
                var token = Advance();
                SkipRedundantOperators();
                if (!Peek().Kind.CanStartPrimary())
                {
                    throw FilterParseException.FromPosition("Missing right side of OR expression", token.Start);
                }

                children.Add(ParseAnd());
            }

            return new FilterOrNode(children, children[0].Start, children[^1].End);
        }

        private FilterNode ParseAnd()
        {
            var children = new List<FilterNode> { ParseUnary() };
            while (true)
            {
                var kind = Peek().Kind;
                if (kind == FilterTokenKind.And)
                {
                    var token = Advance();
                    SkipRedundantOperators();
                    if (!Peek().Kind.CanStartPrimary())
                    {
                        throw FilterParseException.FromPosition("Missing right side of AND expression", token.Start);
                    }

                    children.Add(ParseUnary());
                    continue;
                }

                if (!kind.CanStartPrimary())
                {
                    break;
                }

                var next = Peek();
                if (next.Start == children[^1].End)
                {
                    throw FilterParseException.FromPosition($"Expected whitespace but \"{next.Text}\" found", next.Start);
                }

                children.Add(ParseUnary());
            }

            return children.Count == 1
                ? children[0]
                : new FilterAndNode(children, children[0].Start, children[^1].End);
        }

        private FilterNode ParseUnary()
        {
            if (Peek().Kind != FilterTokenKind.Minus)
            {
                return ParsePrimary();
            }

            var minus = Advance();
            if (!Peek().Kind.CanStartPrimary())
            {
                throw FilterParseException.FromPosition("Missing expression after negation", minus.Start);
            }

            var child = ParseUnary();
            return new FilterNotNode(child, minus.Start, child.End);
        }

        private FilterNode ParsePrimary()
        {
            var token = Peek();
            if (token.Kind == FilterTokenKind.LeftParenthesis)
            {
                var open = Advance();
                if (Peek().Kind == FilterTokenKind.RightParenthesis)
                {
                    var emptyClose = Advance();
                    return new FilterTermNode(string.Empty, open.Start, emptyClose.End);
                }

                var inner = ParseOr();
                var close = ExpectClosing(open);
                return inner with { End = close.End };
            }

            if (token.Kind.CanStartValue())
            {
                var next = Peek(1);
                if (next.Kind == FilterTokenKind.Colon && next.Start == token.End)
                {
                    return ParseClause();
                }

                Advance();
                ValidateTerm(token);
                return new FilterTermNode(token.Value, token.Start, token.End);
            }

            if (token.Kind == FilterTokenKind.Colon)
            {
                throw FilterParseException.FromPosition("Unexpected ':'", token.Start);
            }

            throw token.Kind is FilterTokenKind.And or FilterTokenKind.Or
                ? FilterParseException.FromPosition($"Missing left side of {token.Text} expression", token.Start)
                : FilterParseException.FromPosition($"Unexpected '{token.TextOrEnd()}'", token.Start);
        }

        private FilterNode ParseClause()
        {
            var field = Advance();
            var colon = Advance();
            ValidateField(field);
            return ParseMatcher(field, colon.End);
        }

        private FilterNode ParseMatcher(FilterToken field, int colonEnd)
        {
            var token = Peek();
            if (token.Start != colonEnd || !token.Kind.CanStartMatcher())
            {
                if (token.Kind == FilterTokenKind.Colon && token.Start == colonEnd)
                {
                    throw FilterParseException.FromPosition($"Expected a value but found '{token.Text}'", token.Start);
                }

                throw FilterParseException.FromPosition("Missing value in key:value pair", colonEnd - 1);
            }

            if (token.Kind == FilterTokenKind.Star)
            {
                var star = Advance();
                return new FilterPredicateNode(
                    field.Value,
                    FilterComparisonOperator.Exists,
                    [],
                    field.Start,
                    star.End);
            }

            if (token.Kind.TryGetComparisonOperator(out var comparison))
            {
                var operatorToken = Advance();
                if (Peek().Start != operatorToken.End || !Peek().Kind.CanStartScalarValue())
                {
                    throw FilterParseException.FromPosition("Missing value in range expression", colonEnd - 1);
                }

                var value = ExpectValue();
                if (value.Kind == FilterValueKind.Boolean)
                {
                    throw FilterParseException.FromPosition("Boolean values do not support ordering comparisons", value.Start);
                }

                if (value.Kind != FilterValueKind.Number)
                {
                    throw FilterParseException.FromPosition("Ordering comparisons need a number", value.Start);
                }

                return new FilterPredicateNode(field.Value, comparison, [value], field.Start, value.End);
            }

            if (token.Kind == FilterTokenKind.In)
            {
                Advance();
                var open = Expect(FilterTokenKind.LeftParenthesis, "Expected '(' after 'IN'");
                var values = ParseSetValues(FilterTokenKind.Comma);
                ExpectClosing(open);
                CheckInValues(values);
                return new FilterPredicateNode(field.Value, FilterComparisonOperator.In, values, field.Start, Previous().End);
            }

            if (token.Kind == FilterTokenKind.Range)
            {
                Advance();
                var open = Expect(FilterTokenKind.LeftParenthesis, "Expected '(' after 'RANGE'");
                var values = ParseSetValues(FilterTokenKind.Comma);
                ExpectClosing(open);
                if (values.Length != 2)
                {
                    throw FilterParseException.FromPosition("RANGE requires a minimum and maximum value", open.Start);
                }

                foreach (var value in values)
                {
                    if (value.Kind != FilterValueKind.Number)
                    {
                        throw FilterParseException.FromPosition("RANGE bounds must be numbers", value.Start);
                    }
                }

                return new FilterPredicateNode(field.Value, FilterComparisonOperator.Range, values, field.Start, Previous().End);
            }

            if (token.Kind == FilterTokenKind.LeftParenthesis)
            {
                var open = Advance();
                var values = ParseSetValues(FilterTokenKind.Or);
                ExpectClosing(open);
                CheckInValues(values);
                return new FilterPredicateNode(field.Value, FilterComparisonOperator.In, values, field.Start, Previous().End);
            }

            var equalValue = ExpectValue();
            return new FilterPredicateNode(
                field.Value,
                FilterComparisonOperator.Equal,
                [equalValue],
                field.Start,
                equalValue.End);
        }

        private ImmutableArray<FilterValue> ParseSetValues(FilterTokenKind separator)
        {
            var values = ImmutableArray.CreateBuilder<FilterValue>();
            if (Peek().Kind == FilterTokenKind.RightParenthesis)
            {
                return values.ToImmutable();
            }

            values.Add(ExpectValue());
            while (Peek().Kind != FilterTokenKind.RightParenthesis)
            {
                if (Peek().Kind == FilterTokenKind.End)
                {
                    return values.ToImmutable();
                }

                var token = Peek();
                if (token.Kind != separator)
                {
                    var message = separator == FilterTokenKind.Comma
                        ? "Expected ',' or ')'"
                        : "Expected 'OR' or ')'";
                    throw FilterParseException.FromPosition(message, token.Start);
                }

                Advance();
                if (Peek().Kind is FilterTokenKind.RightParenthesis or FilterTokenKind.End)
                {
                    throw FilterParseException.FromPosition($"Expected a value but found '{Peek().TextOrEnd()}'", Peek().Start);
                }

                values.Add(ExpectValue());
            }

            return values.ToImmutable();
        }

        private FilterValue ExpectValue()
        {
            if (Peek().Kind == FilterTokenKind.Minus)
            {
                return SignedValue(Advance());
            }

            var token = Peek();
            if (!token.Kind.TryGetValueKind(out var kind))
            {
                throw FilterParseException.FromPosition($"Expected a value but found '{token.TextOrEnd()}'", token.Start);
            }

            Advance();
            return new FilterValue(
                kind,
                token.Value,
                token.Kind == FilterTokenKind.String,
                token.HasWildcard,
                token.Start,
                token.End);
        }

        private FilterValue SignedValue(FilterToken sign)
        {
            var run = new List<FilterToken>();
            var end = sign.End;
            var hasWildcard = false;
            while (GluesToRun(end))
            {
                var token = Advance();
                run.Add(token);
                end = token.End;
                hasWildcard |= token.Kind == FilterTokenKind.Star || token.HasWildcard;
            }

            var isNumber = run.Count == 1 && run[0].Kind == FilterTokenKind.Number;
            return new FilterValue(
                isNumber ? FilterValueKind.Number : FilterValueKind.String,
                $"-{string.Concat(run.Select(static token => token.Value))}",
                false,
                hasWildcard,
                sign.Start,
                end);
        }

        private bool GluesToRun(int end)
        {
            var token = Peek();
            if (token.Start != end
                ||
                token.Kind is not (FilterTokenKind.Word or FilterTokenKind.Number or FilterTokenKind.Boolean or FilterTokenKind.Minus or FilterTokenKind.Star))
            {
                return false;
            }

            var after = Peek(1);
            return after.Kind != FilterTokenKind.Colon || after.Start != token.End;
        }

        private void CheckInValues(ImmutableArray<FilterValue> values)
        {
            if (values.Length > 100)
            {
                throw FilterParseException.FromPosition("IN must not contain more than 100 values", values[100].Start);
            }
        }

        private void ValidateField(FilterToken field)
        {
            if (!field.Value.StartsWith('@'))
            {
                return;
            }

            var scope = scopes.FirstOrDefault(scope =>
                field.Value == scope || field.Value.StartsWith($"{scope}.", StringComparison.Ordinal));
            if (scope is null)
            {
                throw FilterParseException.FromPosition("Unknown scope prefix", field.Start);
            }

            var rest = field.Value[scope.Length..];
            if (rest is "" or ".")
            {
                throw FilterParseException.FromPosition($"Expected an attribute name after the {scope} prefix", field.Start);
            }
        }

        private void ValidateTerm(FilterToken term)
        {
            if (!term.Text.StartsWith('@'))
            {
                return;
            }

            var end = 1;
            while (end < term.Text.Length && (char.IsAsciiLetterOrDigit(term.Text[end]) || term.Text[end] == '_'))
            {
                end++;
            }

            var prefix = term.Text[..end];
            var message = scopes.Any(scope => scope.StartsWith(prefix, StringComparison.Ordinal))
                ? "Scope prefixes can only be used in filter expressions"
                : "Unknown scope prefix";
            throw FilterParseException.FromPosition(message, term.Start);
        }

        private void SkipRedundantOperators()
        {
            if (Peek().Kind is FilterTokenKind.And or FilterTokenKind.Or)
            {
                var token = Advance();
                throw FilterParseException.FromPosition($"Unexpected operator '{token.Text}'", token.Start);
            }
        }

        private FilterToken Expect(FilterTokenKind kind, string message)
        {
            if (Peek().Kind != kind)
            {
                throw FilterParseException.FromPosition(message, Peek().Start);
            }

            return Advance();
        }

        private FilterToken ExpectClosing(FilterToken open)
        {
            if (Peek().Kind != FilterTokenKind.RightParenthesis)
            {
                throw FilterParseException.FromPosition("Missing closing parenthesis", open.Start);
            }

            return Advance();
        }

        private FilterToken Peek(int offset = 0)
        {
            var index = _index + offset;
            return index < tokens.Count ? tokens[index] : tokens[^1];
        }

        private FilterToken Previous()
            => _index > 0 ? tokens[_index - 1] : tokens[0];

        private FilterToken Advance()
        {
            var token = Peek();
            if (_index < tokens.Count - 1)
            {
                _index++;
            }

            return token;
        }
    }
}

file static class Extensions
{
    extension(FilterParseException)
    {
        public static FilterParseException FromPosition(string message, int position)
            => new(message, position + 1);
    }

    extension(FilterToken token)
    {
        public string TextOrEnd()
            => token.Text.Length == 0 ? "end of input" : token.Text;
    }

    extension(FilterTokenKind kind)
    {
        public bool CanStartPrimary()
            => kind is FilterTokenKind.Word or FilterTokenKind.String or FilterTokenKind.Number or FilterTokenKind.Boolean or FilterTokenKind.LeftParenthesis or FilterTokenKind.Minus or FilterTokenKind.Colon;

        public bool CanStartMatcher()
            => kind == FilterTokenKind.Star
                || kind.TryGetComparisonOperator(out _)
                || kind is FilterTokenKind.In or FilterTokenKind.Range or FilterTokenKind.LeftParenthesis
                ||
                kind.CanStartScalarValue();

        public bool CanStartScalarValue()
            => kind.CanStartValue() || kind == FilterTokenKind.Minus;

        public bool CanStartValue()
            => kind.TryGetValueKind(out _);

        public bool TryGetComparisonOperator(out FilterComparisonOperator comparison)
        {
            comparison = kind switch
            {
                FilterTokenKind.GreaterThan => FilterComparisonOperator.GreaterThan,
                FilterTokenKind.GreaterThanOrEqual => FilterComparisonOperator.GreaterThanOrEqual,
                FilterTokenKind.LessThan => FilterComparisonOperator.LessThan,
                FilterTokenKind.LessThanOrEqual => FilterComparisonOperator.LessThanOrEqual,
                _ => default
            };
            return kind is FilterTokenKind.GreaterThan or FilterTokenKind.GreaterThanOrEqual or FilterTokenKind.LessThan or FilterTokenKind.LessThanOrEqual;
        }

        public bool TryGetValueKind(out FilterValueKind valueKind)
        {
            valueKind = kind switch
            {
                FilterTokenKind.Word or FilterTokenKind.String => FilterValueKind.String,
                FilterTokenKind.Number => FilterValueKind.Number,
                FilterTokenKind.Boolean => FilterValueKind.Boolean,
                _ => default
            };
            return kind is FilterTokenKind.Word or FilterTokenKind.String or FilterTokenKind.Number or FilterTokenKind.Boolean;
        }
    }
}
