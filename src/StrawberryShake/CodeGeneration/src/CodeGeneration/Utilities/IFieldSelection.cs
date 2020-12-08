using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public interface IFieldSelection
    {
        string ResponseName { get; }

        IOutputField Field { get; }

        FieldNode FieldSyntax { get; }

        Path Path { get; }
    }
}
