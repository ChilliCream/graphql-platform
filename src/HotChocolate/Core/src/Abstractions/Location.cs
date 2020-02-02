using System;

namespace HotChocolate
{
    public readonly struct Location
    {
        public Location(int line, int column)
        {
            if (line < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line), line,
                    "line is a 1-base index and cannot be less than one.");
            }

            if (column < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(column), column,
                    "column is a 1-base index and cannot be less than one.");
            }

            Line = line;
            Column = column;
        }

        public int Line { get; }

        public int Column { get; }
    }
}
