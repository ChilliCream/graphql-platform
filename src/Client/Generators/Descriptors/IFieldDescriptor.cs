using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IFieldDescriptor
    {
        string ResponseName { get; }
        IOutputField Field { get; }
        FieldNode Selection { get; }
        IType Type { get; }
    }
}
