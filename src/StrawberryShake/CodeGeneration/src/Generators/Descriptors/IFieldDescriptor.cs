using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IFieldDescriptor
    {
        string ResponseName { get; }

        Path Path { get; }

        IOutputField Field { get; }

        FieldNode Selection { get; }

        IType Type { get; }
    }
}
