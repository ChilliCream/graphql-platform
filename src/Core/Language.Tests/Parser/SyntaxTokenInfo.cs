using System;
using System.Text;

namespace HotChocolate.Language
{
    public sealed class SyntaxTokenInfo
    {
        public SyntaxTokenInfo(
            TokenKind kind,
            int start,
            int end,
            int line,
            int column,
            string value)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Value = value;
        }

        /// <summary>
        /// Gets the kind of <see cref="SyntaxToken" />.
        /// </summary>
        public TokenKind Kind { get; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the 1-indexed line number on which this
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-indexed column number at which this
        /// <see cref="SyntaxToken" /> begins.
        /// </summary>
        public int Column { get; }

        public string Value { get; }

        public unsafe static SyntaxTokenInfo FromReader(in Utf8GraphQLReader reader)
        {
            string value = null;

            ReadOnlySpan<byte> token = reader.GraphQLData.Slice(
                reader.Start, reader.End - reader.Start);

            fixed (byte* b = token)
            {
                value = Encoding.UTF8.GetString(b, token.Length);
            }

            return new SyntaxTokenInfo(
                reader.Kind,
                reader.Start,
                reader.End,
                reader.Line,
                reader.Column,
                value);
        }
    }
}
