using System.Collections.Generic;
namespace HotChocolate.Types
{
    public interface IHasContextData
    {
        IReadOnlyDictionary<string, object> ContextData { get; }
    }
}
