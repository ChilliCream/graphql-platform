using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class Location
        : ILocation
    {
        public Location(
            int column, 
            int line, 
            int start, 
            int end)
        {
            Column = column;
            Line = line;
            Start = start;
            End = end;
        }

        public int Column { get; }

        public int Line { get; }

        public int Start { get; }

        public int End { get; }
    }
}
