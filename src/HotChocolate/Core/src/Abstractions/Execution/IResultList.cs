using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IResultList
        : IReadOnlyList<object?>
        , IResultData
    {
    }
}
