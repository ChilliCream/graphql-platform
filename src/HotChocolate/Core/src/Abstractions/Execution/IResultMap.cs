using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IResultMap
        : IReadOnlyList<ResultValue>
        , IResultData
        , IReadOnlyDictionary<string, object?>
    {
    }
}
