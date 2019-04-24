using System;

namespace HotChocolate.Language
{
    internal static class BlockStringHelper
    {
        public static int CountLines(in ReadOnlySpan<byte> data)
        {
            int lines = 0;
            int position = 0;

            while (position < data.Length)
            {
                GoToNextLine(in data, ref position);
                lines++;
            }

            return lines;
        }

        public static void TrimBlockStringToken(
            in ReadOnlySpan<byte> data,
            ref Span<byte> trimmedData)
        {
            int position = 0;
            GoToNextLine(in data, ref position);

            // Remove common indentation from all lines but first.
            int? commonIndent = null;
            while (position < data.Length)
            {
                int indent = GetLeadingWhitespace(in data, ref position);
                int lineLength = indent + GoToNextLine(in data, ref position);

                if (
                  indent < lineLength &&
                  (commonIndent == null || indent < commonIndent)
                )
                {
                    commonIndent = indent;
                    if (commonIndent == 0)
                    {
                        break;
                    }
                }
            }

            ReadOnlySpan<byte> line;

            if (commonIndent.HasValue)
            {
                position = 0;
                line = GetNextLine(in data, ref position);
                line.CopyTo(trimmedData);
                int writePosition = line.Length;
                trimmedData[writePosition] = ReaderHelper.NewLine;

                while (position < data.Length)
                {
                    line = GetNextLine(in data, ref position);
                    if (commonIndent.Value > 0)
                    {
                        line = line.Slice(commonIndent.Value);
                    }

                    for (int i = 0; i < line.Length; i++)
                    {
                        trimmedData[++writePosition] = line[i];
                    }

                    trimmedData[++writePosition] = ReaderHelper.NewLine;
                }
            }

            // Remove leading and trailing blank lines.
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

            position = trimmedData.Length - 1;
            while (trimmedData.Length > 0)
            {
                line = GetNextLineReverse(in trimmedData, ref position);
                if (line.Length == 0)
                {
                    trimmedData = trimmedData.Slice(
                        0, trimmedData.Length - position);
                    position = 0;
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
            int i = 0;
            while (position < chunk.Length
                && (chunk[position] == ReaderHelper.Space
                    || chunk[position] == ReaderHelper.Space))
            {
                i++;
                position++;
            }
            return i;
        }

        private static ReadOnlySpan<byte> GetNextLine(
            in ReadOnlySpan<byte> data,
            ref int position)
        {
            int start = position;
            int length = GoToNextLine(in data, ref position);
            return data.Slice(start, length);
        }

        private static Span<byte> GetNextLine(
            in Span<byte> data,
            ref int position)
        {
            int start = position;
            int length = GoToNextLine(in data, ref position);
            return data.Slice(start, length);
        }

        private static Span<byte> GetNextLineReverse(
            in Span<byte> data,
            ref int position)
        {
            int start = position;
            GoToNextLine(in data, ref position);
            int length = start - position;
            return data.Slice(start, length);
        }

        private static int GoToNextLine(
            in ReadOnlySpan<byte> data,
            ref int position)
        {
            int i = 0;
            while (position < data.Length)
            {
                if (data[position] == ReaderHelper.NewLine)
                {
                    if (position < data.Length
                        && data[position + 1] == ReaderHelper.Return)
                    {
                        position++;
                    }
                    position++;
                    break;
                }

                if (data[position] == ReaderHelper.Return)
                {
                    if (position < data.Length
                        && data[position + 1] == ReaderHelper.NewLine)
                    {
                        position++;
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
            int i = 0;
            while (position < data.Length)
            {
                if (data[position] == ReaderHelper.NewLine)
                {
                    if (position < data.Length
                        && data[position + 1] == ReaderHelper.Return)
                    {
                        position++;
                    }
                    position++;
                    break;
                }

                if (data[position] == ReaderHelper.Return)
                {
                    if (position < data.Length
                        && data[position + 1] == ReaderHelper.NewLine)
                    {
                        position++;
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
            int i = 0;
            while (position >= 0)
            {
                if (data[position] == ReaderHelper.NewLine)
                {
                    if (position > 0
                        && data[position - 1] == ReaderHelper.Return)
                    {
                        position--;
                    }
                    position--;
                    break;
                }

                if (data[position] == ReaderHelper.Return)
                {
                    if (position > 0
                        && data[position - 1] == ReaderHelper.NewLine)
                    {
                        position--;
                    }
                    position--;
                    break;
                }
                position--;
                i++;
            }
            return i;
        }
    }
}
