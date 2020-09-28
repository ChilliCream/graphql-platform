using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IResultMapList
        : IReadOnlyList<IResultMap?>
        , IResultData
    {
    }
}
