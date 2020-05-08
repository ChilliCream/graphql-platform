using System;
using HotChocolate.Properties;

namespace HotChocolate
{
    public readonly struct Location
    {
        public Location(int line, int column)
        {
            if (line < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line),
                    line,
                    AbstractionResources.Location_Line_Is_1_Based);
            }

            if (column < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(column),
                    column,
                    AbstractionResources.Location_Column_Is_1_Based);
            }

            Line = line;
            Column = column;
        }

        public int Line { get; }

        public int Column { get; }
    }
}
