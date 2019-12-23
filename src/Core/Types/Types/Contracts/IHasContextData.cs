using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types
{
    public interface IHasContextData
    {
        IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
