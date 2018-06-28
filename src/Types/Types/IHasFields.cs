using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IHasFields
    {
        IReadOnlyDictionary<string, Field> Fields { get; }
    }
}
