using System;
using Newtonsoft.Json;

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

        [JsonProperty("line", Order = 0)]
        public int Line { get; }

        [JsonProperty("column", Order = 1)]
        public int Column { get; }
    }
}
