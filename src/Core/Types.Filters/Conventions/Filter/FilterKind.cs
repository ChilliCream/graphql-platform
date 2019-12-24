using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public enum FilterKind : byte
    {
        /// <summary>
        /// This filter is allowed for string filters
        /// </summary>
        String = 0x1,

        /// <summary>
        /// This filter is allowed for comparable filters
        /// </summary>
        Comparable = 0x2,

        /// <summary>
        /// This filter is allowed for boolean filters
        /// </summary>
        Boolean = 0x4,

        /// <summary>
        /// This filter is allowed for array filters
        /// </summary>
        Array = 0x8,

        /// <summary>
        /// This filter is allowed for object filters
        /// </summary>
        Object = 0x10
    }
}
