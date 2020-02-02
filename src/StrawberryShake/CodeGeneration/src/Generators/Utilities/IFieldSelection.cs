using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    public interface IFieldSelection
    {
        string ResponseName { get; }
        IOutputField Field { get; }
        FieldNode Selection { get; }
        Path Path { get; }
    }
}
