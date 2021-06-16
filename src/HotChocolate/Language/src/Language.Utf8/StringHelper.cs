using System;
using System.Text;

namespace HotChocolate.Language
{
    internal static class StringHelper
    {
        public static readonly UTF8Encoding UTF8Encoding = new();

        public static void TrimStringToken(
            ref ReadOnlySpan<byte> data)
        {
            var position = 0;
            var temp = GetLeadingWhitespace(in data, ref position);

            if (data.Length >= temp)
            {
                data = data.Slice(temp);
            }

            temp = GetTrailingWhitespace(in data);

            if (temp == data.Length)
            {
                data = data.Slice(0, temp);
            }
        }

        public static void TrimBlockStringToken(
            in ReadOnlySpan<byte> data,
            ref Span<byte> trimmedData)
        {
            var position = 0;
            GoToNextLine(in data, ref position);

            // Remove common indentation from all lines but first.
            int? commonIndent = null;
            while (position < data.Length)
            {
                var indent = GetLeadingWhitespace(in data, ref position);
                var lineLength = indent + GoToNextLine(in data, ref position);

                if (lineLength > 0 &&
                    indent <= lineLength &&
                    (commonIndent == null || indent < commonIndent))
                {
                    commonIndent = indent;
                    if (commonIndent == 0)
                    {
                        break;
                    }
                }
            }

            var trim = commonIndent is > 0;

            position = 0;
            ReadOnlySpan<byte> line = GetNextLine(in data, ref position);
            line.CopyTo(trimmedData);
            var next = line.Length;
            var writePosition = next - 1;

            if (trimmedData.Length > next)
            {
                trimmedData[next] = GraphQLConstants.LineFeed;
                writePosition = next;
            }

            while (position < data.Length)
            {
                line = GetNextLine(in data, ref position);
                if (trim && commonIndent.HasValue && line.Length >= commonIndent.Value)
                {
                    line = line.Slice(commonIndent.Value);
                }

                for (var i = 0; i < line.Length; i++)
                {
                    trimmedData[++writePosition] = line[i];
                }

                next = writePosition + 1;
                if (trimmedData.Length > next)
                {
                    trimmedData[next] = GraphQLConstants.LineFeed;
                    writePosition = next;
                }
            }

            trimmedData = trimmedData.Slice(0, writePosition + 1);

            // Remove leading blank lines.
            position = 0;
            while (position < trimmedData.Length)
            {
                line = GetNextLine(in trimmedData, ref position);
                if (line.Length == 0)
                {
                    trimmedData = trimmedData.Slice(position);
                    position = 0;
                }
                else
                {
                    break;
                }
            }

            // Remove trailing blank lines.
            position = trimmedData.Length - 1;
            while (trimmedData.Length > 0)
            {
                line = GetNextLineReverse(in trimmedData, ref position);
                if (line.Length == 0 && position > 0)
                {
                    trimmedData = trimmedData.Slice(0, position + 1);
                    position = trimmedData.Length - 1;
                }
                else
                {
                    break;
                }
            }
        }

        private static int GetLeadingWhitespace(
            in ReadOnlySpan<byte> chunk,
            ref int position)
        {
            var i = 0;
            while (position < chunk.Length
                && (chunk[position] == GraphQLConstants.Space
                    || chunk[position] == GraphQLConstants.HorizontalTab))
            {
                i++;
                position++;
            }
            return i;
        }

        private static int GetTrailingWhitespace(
            in ReadOnlySpan<byte> chunk)
        {
            var position = chunk.Length - 1;
            while (position > 0
                && (chunk[position] == GraphQLConstants.Space
                    || chunk[position] == GraphQLConstants.HorizontalTab))
            {
                position--;
            }
            return position;
        }

        private static ReadOnlySpan<byte> GetNextLine(
            in ReadOnlySpan<byte> data,
            ref int position)
        {
            var start = position;
            var length = GoToNextLine(in data, ref position);
            return data.Slice(start, length);
        }

        private static Span<byte> GetNextLine(
            in Span<byte> data,
            ref int position)
        {
            var start = position;
            var length = GoToNextLine(in data, ref position);
            return data.Slice(start, length);
        }

        private static Span<byte> GetNextLineReverse(
            in Span<byte> data,
            ref int position)
        {
            var length = GoToPreviousLine(in data, ref position);
            return data.Slice(position, length);
        }

        private static int GoToNextLine(
            in ReadOnlySpan<byte> data,
            ref int position)
        {
            var i = 0;
            while (position < data.Length)
            {
                if (data[position] == GraphQLConstants.LineFeed)
                {
                    position++;
                    break;
                }

                if (data[position] == GraphQLConstants.Return)
                {
                    var next = position + 1;
                    if (next < data.Length
                        && data[next] == GraphQLConstants.LineFeed)
                    {
                        position = next;
                    }
                    position++;
                    break;
                }

                position++;
                i++;
            }
            return i;
        }

        private static int GoToNextLine(
            in Span<byte> data,
            ref int position)
        {
            var i = 0;
            while (position < data.Length)
            {
                if (data[position] == GraphQLConstants.LineFeed)
                {
                    position++;
                    break;
                }

                if (data[position] == GraphQLConstants.Return)
                {
                    var next = position + 1;
                    if (next < data.Length
                        && data[next] == GraphQLConstants.LineFeed)
                    {
                        position = next;
                    }
                    position++;
                    break;
                }

                position++;
                i++;
            }
            return i;
        }

        private static int GoToPreviousLine(
            in Span<byte> data,
            ref int position)
        {
            var i = 0;
            while (position >= 0)
            {
                if (data[position] == GraphQLConstants.LineFeed)
                {
                    position--;
                    break;
                }

                if (data[position] == GraphQLConstants.Return)
                {
                    var next = position - 1;
                    if (next > 0
                        && data[next] == GraphQLConstants.LineFeed)
                    {
                        position = next;
                    }
                    position--;
                    break;
                }

                if (position == 0)
                {
                    break;
                }

                position--;
                i++;
            }
            return i;
        }
    }
}
