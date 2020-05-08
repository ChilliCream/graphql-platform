using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class ResultMapList
        : List<IResultMap>
        , IResultMapList
    {
        public IResultData? Parent { get; set; }

        /// <summary>
        /// Specifies that the elements of this list are nullable.
        /// </summary>
        public bool Nullable { get; set; }
    }
}
