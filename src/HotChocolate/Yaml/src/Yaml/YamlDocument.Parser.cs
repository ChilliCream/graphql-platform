using System.Text;

namespace HotChocolate.Text.Yaml;

public sealed partial class YamlDocument
{
    private ref struct Parser
    {
        private readonly ReadOnlySpan<byte> _yaml;
        private readonly List<Line> _lines = [];
        private readonly List<DbRow> _rows = [];
        private int _index;

        private Parser(ReadOnlySpan<byte> yaml)
        {
            _yaml = yaml;
        }

        public static DbRow[] Parse(ReadOnlySpan<byte> yaml)
        {
            var parser = new Parser(yaml);
            parser.ReadLines();
            parser.ParseDocument();
            return [.. parser._rows];
        }

        private void ReadLines()
        {
            var offset = _yaml.StartsWith("\uFEFF"u8) ? 3 : 0;
            var number = 1;

            while (offset < _yaml.Length)
            {
                var lineStart = offset;
                var length = _yaml[offset..].IndexOf((byte)'\n');
                var end = length < 0 ? _yaml.Length : offset + length;
                offset = end + 1;

                if (end > lineStart && _yaml[end - 1] == (byte)'\r')
                {
                    end--;
                }

                var start = lineStart;

                while (start < end && _yaml[start] == (byte)' ')
                {
                    start++;
                }

                var contentStart = start;

                while (contentStart < end && _yaml[contentStart] is (byte)' ' or (byte)'\t')
                {
                    contentStart++;
                }

                if (contentStart == end || _yaml[contentStart] == (byte)'#')
                {
                    number++;
                    continue;
                }

                if (start < contentStart)
                {
                    throw ThrowHelper.TabIndentation(number, start - lineStart + 1);
                }

                while (_yaml[end - 1] is (byte)' ' or (byte)'\t')
                {
                    end--;
                }

                if (start == lineStart && IsDocumentMarker(_yaml[start..end]))
                {
                    throw ThrowHelper.DocumentMarkersNotSupported(number, 1);
                }

                _lines.Add(new Line(lineStart, start, end, number));
                number++;
            }
        }

        private void ParseDocument()
        {
            if (_lines.Count == 0)
            {
                AddRow(YamlValueKind.Null, 0, 0, quoted: false, line: 1, column: 1);
                return;
            }

            ParseNode(_lines[0].Indent);

            if (_index < _lines.Count)
            {
                var line = _lines[_index];
                throw ThrowHelper.UnexpectedIndentation(line.Number, line.Indent + 1);
            }
        }

        private void ParseNode(int indent)
        {
            var line = _lines[_index];

            if (IsSequenceEntry(line))
            {
                ParseSequence(indent);
            }
            else if (TryReadKey(line, out _, out _, out _))
            {
                ParseMapping(indent);
            }
            else
            {
                ParseInlineValue(line.Start, line);
                _index++;
            }
        }

        private void ParseSequence(int indent)
        {
            var first = _lines[_index];
            var rowIndex = AddRow(YamlValueKind.Array, first.Start, 0, false, first.Number, first.Indent + 1);
            var count = 0;

            while (_index < _lines.Count)
            {
                var line = _lines[_index];

                if (line.Indent != indent || !IsSequenceEntry(line))
                {
                    break;
                }

                count++;

                var valueStart = SkipWhitespace(line.Start + 1, line.End);

                if (valueStart == line.End || _yaml[valueStart] == (byte)'#')
                {
                    var number = line.Number;
                    var column = line.Indent + 2;
                    _index++;

                    if (_index < _lines.Count && _lines[_index].Indent > indent)
                    {
                        ParseNode(_lines[_index].Indent);
                    }
                    else
                    {
                        AddRow(YamlValueKind.Null, 0, 0, false, number, column);
                    }
                }
                else
                {
                    line.Start = valueStart;
                    _lines[_index] = line;
                    ParseNode(line.Indent);
                }

                EnsureNotIndented(indent);
            }

            CompleteContainer(rowIndex, count);
        }

        private void ParseMapping(int indent)
        {
            var first = _lines[_index];
            var rowIndex = AddRow(YamlValueKind.Object, first.Start, 0, false, first.Number, first.Indent + 1);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;

            while (_index < _lines.Count)
            {
                var line = _lines[_index];

                if (line.Indent != indent || IsSequenceEntry(line))
                {
                    break;
                }

                if (!TryReadKey(line, out var keyStart, out var keyEnd, out var valueStart))
                {
                    throw ThrowHelper.ExpectedKey(line.Number, line.Indent + 1);
                }

                var quoted = _yaml[keyStart] == (byte)'"';

                if (!quoted)
                {
                    ValidatePlainStart(keyStart, line);
                }

                var key = quoted
                    ? Unescape(_yaml[(keyStart + 1)..(keyEnd - 1)])
                    : Encoding.UTF8.GetString(_yaml[keyStart..keyEnd]);

                if (!keys.Add(key))
                {
                    throw ThrowHelper.DuplicateKey(key, line.Number, line.Indent + 1);
                }

                if (quoted)
                {
                    AddRow(YamlValueKind.String, keyStart + 1, keyEnd - keyStart - 2, true, line.Number, line.Indent + 1);
                }
                else
                {
                    AddRow(YamlValueKind.String, keyStart, keyEnd - keyStart, false, line.Number, line.Indent + 1);
                }

                count++;

                valueStart = SkipWhitespace(valueStart, line.End);

                if (valueStart == line.End || _yaml[valueStart] == (byte)'#')
                {
                    _index++;

                    if (_index < _lines.Count && _lines[_index].Indent > indent)
                    {
                        ParseNode(_lines[_index].Indent);
                    }
                    else if (_index < _lines.Count
                        && _lines[_index].Indent == indent
                        && IsSequenceEntry(_lines[_index]))
                    {
                        ParseSequence(indent);
                    }
                    else
                    {
                        AddRow(YamlValueKind.Null, 0, 0, false, line.Number, valueStart - line.LineStart + 1);
                    }
                }
                else
                {
                    ParseInlineValue(valueStart, line);
                    _index++;
                }

                EnsureNotIndented(indent);
            }

            CompleteContainer(rowIndex, count);
        }

        private void ParseInlineValue(int start, Line line)
        {
            var column = start - line.LineStart + 1;

            switch (_yaml[start])
            {
                case (byte)'"':
                    var end = FindClosingQuote(start, line);
                    EnsureEndOfLine(end + 1, line);
                    AddRow(YamlValueKind.String, start + 1, end - start - 1, true, line.Number, column);
                    return;

                case (byte)'[' when start + 1 < line.End && _yaml[start + 1] == (byte)']':
                    EnsureEndOfLine(start + 2, line);
                    CompleteContainer(AddRow(YamlValueKind.Array, start, 0, false, line.Number, column), 0);
                    return;

                case (byte)'{' when start + 1 < line.End && _yaml[start + 1] == (byte)'}':
                    EnsureEndOfLine(start + 2, line);
                    CompleteContainer(AddRow(YamlValueKind.Object, start, 0, false, line.Number, column), 0);
                    return;
            }

            ValidatePlainStart(start, line);

            var plainEnd = FindPlainEnd(start, line.End);

            for (var i = start; i < plainEnd; i++)
            {
                if (_yaml[i] == (byte)':' && (i + 1 == plainEnd || _yaml[i + 1] is (byte)' ' or (byte)'\t'))
                {
                    throw ThrowHelper.ColonInPlainScalar(line.Number, i - line.LineStart + 1);
                }
            }

            var text = _yaml[start..plainEnd];
            AddRow(ResolvePlainKind(text), start, text.Length, false, line.Number, column);
        }

        private readonly bool TryReadKey(Line line, out int keyStart, out int keyEnd, out int valueStart)
        {
            keyStart = line.Start;

            if (_yaml[keyStart] == (byte)'"')
            {
                var quote = FindClosingQuote(keyStart, line);
                keyEnd = quote + 1;
                valueStart = keyEnd + 1;
                return keyEnd < line.End
                    && _yaml[keyEnd] == (byte)':'
                    && (valueStart == line.End || _yaml[valueStart] is (byte)' ' or (byte)'\t');
            }

            for (var i = keyStart; i < line.End; i++)
            {
                var current = _yaml[i];

                if (current == (byte)'#' && i > keyStart && _yaml[i - 1] is (byte)' ' or (byte)'\t')
                {
                    break;
                }

                if (current == (byte)':' && (i + 1 == line.End || _yaml[i + 1] is (byte)' ' or (byte)'\t'))
                {
                    keyEnd = i;

                    while (keyEnd > keyStart && _yaml[keyEnd - 1] is (byte)' ' or (byte)'\t')
                    {
                        keyEnd--;
                    }

                    valueStart = i + 1;
                    return true;
                }
            }

            keyEnd = 0;
            valueStart = 0;
            return false;
        }

        private readonly int FindClosingQuote(int start, Line line)
        {
            for (var i = start + 1; i < line.End; i++)
            {
                switch (_yaml[i])
                {
                    case (byte)'"':
                        return i;

                    case (byte)'\\':
                        i += ValidateEscape(i, line);
                        break;
                }
            }

            throw ThrowHelper.UnterminatedString(line.Number, start - line.LineStart + 1);
        }

        private readonly int ValidateEscape(int start, Line line)
        {
            if (start + 1 < line.End)
            {
                switch (_yaml[start + 1])
                {
                    case (byte)'"' or (byte)'\\' or (byte)'/' or (byte)'b' or (byte)'f'
                        or (byte)'n' or (byte)'r' or (byte)'t':
                        return 1;

                    case (byte)'u' when start + 5 < line.End
                        && IsHexDigits(_yaml.Slice(start + 2, 4)):
                        return 5;
                }
            }

            throw ThrowHelper.InvalidEscapeSequence(line.Number, start - line.LineStart + 1);
        }

        private readonly void ValidatePlainStart(int start, Line line)
        {
            var current = _yaml[start];
            var column = start - line.LineStart + 1;

            switch (current)
            {
                case (byte)'&':
                    throw ThrowHelper.AnchorsNotSupported(line.Number, column);

                case (byte)'*':
                    throw ThrowHelper.AliasesNotSupported(line.Number, column);

                case (byte)'!':
                    throw ThrowHelper.TagsNotSupported(line.Number, column);

                case (byte)'|' or (byte)'>':
                    throw ThrowHelper.BlockScalarsNotSupported(line.Number, column);

                case (byte)'\'':
                    throw ThrowHelper.SingleQuotedScalarsNotSupported(line.Number, column);

                case (byte)'[' or (byte)'{':
                    throw ThrowHelper.FlowCollectionsNotSupported(line.Number, column);

                case (byte)']' or (byte)'}' or (byte)',' or (byte)'%' or (byte)'@' or (byte)'`':
                    throw ThrowHelper.UnexpectedCharacter((char)current, line.Number, column);

                case (byte)'-' or (byte)'?' or (byte)':'
                    when start + 1 == line.End || _yaml[start + 1] is (byte)' ' or (byte)'\t':
                    throw ThrowHelper.UnexpectedCharacter((char)current, line.Number, column);
            }
        }

        private readonly void EnsureEndOfLine(int start, Line line)
        {
            var end = SkipWhitespace(start, line.End);

            if (end < line.End && (end == start || _yaml[end] != (byte)'#'))
            {
                throw ThrowHelper.UnexpectedCharacter((char)_yaml[end], line.Number, end - line.LineStart + 1);
            }
        }

        private readonly void EnsureNotIndented(int indent)
        {
            if (_index < _lines.Count && _lines[_index].Indent > indent)
            {
                var line = _lines[_index];
                throw ThrowHelper.UnexpectedIndentation(line.Number, line.Indent + 1);
            }
        }

        private readonly int FindPlainEnd(int start, int end)
        {
            for (var i = start + 1; i < end; i++)
            {
                if (_yaml[i] == (byte)'#' && _yaml[i - 1] is (byte)' ' or (byte)'\t')
                {
                    end = i;
                    break;
                }
            }

            while (_yaml[end - 1] is (byte)' ' or (byte)'\t')
            {
                end--;
            }

            return end;
        }

        private readonly int SkipWhitespace(int start, int end)
        {
            while (start < end && _yaml[start] is (byte)' ' or (byte)'\t')
            {
                start++;
            }

            return start;
        }

        private readonly bool IsSequenceEntry(Line line)
            => _yaml[line.Start] == (byte)'-'
                && (line.Start + 1 == line.End || _yaml[line.Start + 1] is (byte)' ' or (byte)'\t');

        private static bool IsDocumentMarker(ReadOnlySpan<byte> content)
            => (content.StartsWith("---"u8) || content.StartsWith("..."u8))
                && (content.Length == 3 || content[3] is (byte)' ' or (byte)'\t');

        private static bool IsHexDigits(ReadOnlySpan<byte> text)
        {
            foreach (var current in text)
            {
                if (!char.IsAsciiHexDigit((char)current))
                {
                    return false;
                }
            }

            return true;
        }

        private static YamlValueKind ResolvePlainKind(ReadOnlySpan<byte> text)
        {
            if (text.SequenceEqual("null"u8))
            {
                return YamlValueKind.Null;
            }

            if (text.SequenceEqual("true"u8))
            {
                return YamlValueKind.True;
            }

            if (text.SequenceEqual("false"u8))
            {
                return YamlValueKind.False;
            }

            return IsJsonNumber(text) ? YamlValueKind.Number : YamlValueKind.String;
        }

        private static bool IsJsonNumber(ReadOnlySpan<byte> text)
        {
            var i = 0;

            if (i < text.Length && text[i] == (byte)'-')
            {
                i++;
            }

            if (i == text.Length)
            {
                return false;
            }

            if (text[i] == (byte)'0')
            {
                i++;
            }
            else if (char.IsAsciiDigit((char)text[i]))
            {
                i = SkipDigits(text, i);
            }
            else
            {
                return false;
            }

            if (i < text.Length && text[i] == (byte)'.')
            {
                var fractionStart = i + 1;
                i = SkipDigits(text, fractionStart);

                if (i == fractionStart)
                {
                    return false;
                }
            }

            if (i < text.Length && text[i] is (byte)'e' or (byte)'E')
            {
                i++;

                if (i < text.Length && text[i] is (byte)'+' or (byte)'-')
                {
                    i++;
                }

                var exponentStart = i;
                i = SkipDigits(text, exponentStart);

                if (i == exponentStart)
                {
                    return false;
                }
            }

            return i == text.Length;
        }

        private static int SkipDigits(ReadOnlySpan<byte> text, int start)
        {
            while (start < text.Length && char.IsAsciiDigit((char)text[start]))
            {
                start++;
            }

            return start;
        }

        private int AddRow(YamlValueKind kind, int location, int length, bool quoted, int line, int column)
        {
            _rows.Add(
                new DbRow
                {
                    Kind = kind,
                    IsQuoted = quoted,
                    Location = location,
                    Length = length,
                    Line = line,
                    Column = column,
                    NumberOfRows = 1
                });

            return _rows.Count - 1;
        }

        private readonly void CompleteContainer(int rowIndex, int childCount)
        {
            var row = _rows[rowIndex];
            row.NumberOfRows = _rows.Count - rowIndex;
            row.ChildCount = childCount;
            _rows[rowIndex] = row;
        }
    }

    private struct Line(int lineStart, int start, int end, int number)
    {
        public readonly int LineStart = lineStart;
        public readonly int End = end;
        public readonly int Number = number;
        public int Start = start;

        public readonly int Indent => Start - LineStart;
    }
}
