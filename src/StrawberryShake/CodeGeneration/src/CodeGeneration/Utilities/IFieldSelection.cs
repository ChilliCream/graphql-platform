using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public interface IFieldSelection
    {
        string ResponseName { get; }
        IOutputField Field { get; }
        FieldNode Selection { get; }
        Path Path { get; }
    }
}
