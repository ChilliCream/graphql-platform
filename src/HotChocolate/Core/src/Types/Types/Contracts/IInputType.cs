using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputType
        : ISerializableType
        , IParsableType
        , IHasRuntimeType
    {
    }
}
