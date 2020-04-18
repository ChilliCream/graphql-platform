using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class ResultMapList
        : List<IResultMap>
        , IResultMapList
    {
        public IResultData? Parent { get; set; }
    }
}
