using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public enum FilterKind : byte
    {
        /// <summary>
        /// All Object Filters
        /// </summary>
        Ignored = 0x0,

        /// <summary>
        /// All String Filters
        /// </summary>
        String = 0x1,

        /// <summary>
        /// All Comparable Filters
        /// </summary>
        Comparable = 0x2,

        /// <summary>
        /// All Boolean Filters
        /// </summary>
        Boolean = 0x4,

        /// <summary>
        /// All Array Filters
        /// </summary>
        Array = 0x8,

        /// <summary>
        /// All Object Filters
        /// </summary>
        Object = 0x10
    }
}
