using System;

namespace HotChocolate.Language
{
    internal class LexerState
    {
        public int Position;
        public int Line = 1;
        public int LineStart = 0;
        public int Column = 1;
        public string SourceText;

        public void NewLine()
        {
            Line++;
            LineStart = Position;
            UpdateColumn();
        }

        public void NewLine(int lines)
        {
            if (lines < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lines), "must be greater or equal to 1.");
            }

            Line += lines;
            LineStart = Position;
            UpdateColumn();
        }

        public void UpdateColumn()
        {
            Column = 1 + Position - LineStart;
        }

        public bool IsEndOfStream()
        {
            return !(Position < SourceText.Length);
        }
    }
}