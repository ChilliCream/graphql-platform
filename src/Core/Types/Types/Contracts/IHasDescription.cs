using System.Collections.Generic;
namespace HotChocolate.Types
{
    public interface IHasDescription
    {
        string Description { get; }
    }

    public interface IHasContextData
    {
        IReadOnlyDictionary<string, object> ContextData { get; }
    }
}
